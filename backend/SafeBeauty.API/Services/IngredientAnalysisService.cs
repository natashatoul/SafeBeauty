
using Microsoft.EntityFrameworkCore;
using SafeBeauty.API.Data;
using SafeBeauty.API.DTOs;
using SafeBeauty.API.Models;
using SafeBeauty.API.Models.Enums;

namespace SafeBeauty.API.Services;

public class IngredientAnalysisService
{
    private readonly SafeBeautyDbContext _context;
    private readonly HuggingFaceService _huggingFace;
    private readonly AiSummaryService _aiSummary;
    private const int MaxUnknownIngredientsForAiClassification = 5;

    // Constructor injection - SafeBeautyDbContext is provided by ASP.NET DI container
    public IngredientAnalysisService(SafeBeautyDbContext context, HuggingFaceService huggingFace, AiSummaryService aiSummary)
    {
        _context = context;
        _huggingFace = huggingFace;
        _aiSummary = aiSummary;
    }

    public async Task<AnalyseResponse> AnalyseAsync(
        List<string> ingredients,
        List<string>? userConditions = null,
        string? ageGroup = null,
        string? gender = null)
    {
        var selectedConditions = ParseConditions(userConditions);
        var parsedIngredients = await ParseAndNormalizeAsync(ingredients);
        var resolution = await ResolveIngredientsAsync(parsedIngredients);
        var response = await EnrichAsync(resolution, selectedConditions);
        response.AiSummary = await BuildSummaryAsync(response, userConditions, ageGroup, gender);
        return response;
    }

    private static HashSet<Condition> ParseConditions(List<string>? userConditions)
    {
        // Convert frontend condition strings into the backend enum values.
        // Unknown strings are ignored, so an old localStorage value cannot break
        // the whole analysis request.
        return (userConditions ?? new List<string>())
            .Select(c => Enum.TryParse<Condition>(c, out var condition)
                ? condition
                : (Condition?)null)
            .Where(c => c.HasValue)
            .Select(c => c!.Value)
            .ToHashSet();
    }

    private async Task<ParsedIngredients> ParseAndNormalizeAsync(List<string> ingredients)
    {
        // Treat each request item defensively: older clients and copied labels
        // may send a complete bullet-separated list as one array element.
        var parsedIngredients = IngredientListParser.Parse(ingredients);
        var needsInferredBoundaries = parsedIngredients.Count <= 2
            && ingredients.Any(IngredientListParser.LooksLikeUnseparatedList);

        if (needsInferredBoundaries)
        {
            // Only the unusual no-separator format needs the full vocabulary.
            // Normal comma/bullet lists keep the existing targeted DB query.
            var knownNames = await _context.Ingredients
                .AsNoTracking()
                .Select(ingredient => ingredient.InciName)
                .ToListAsync();

            parsedIngredients = parsedIngredients
                .SelectMany(entry => IngredientListParser.SegmentByKnownNames(entry, knownNames))
                .ToList();
        }

        var normalizedIngredients = parsedIngredients
            .Select(IngredientNormalizer.Normalize)
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        // Include legacy punctuation variants so an existing database created
        // before the current normalizer still resolves values such as
        // "ALCOHOL DENAT.". The result is keyed with the current normalizer below.
        var lookupCandidates = normalizedIngredients
            .SelectMany(name => new[] { name, $"{name}." })
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return new ParsedIngredients(normalizedIngredients, lookupCandidates);
    }

    private async Task<IngredientResolution> ResolveIngredientsAsync(ParsedIngredients parsed)
    {
        var knownIngredients = await _context.Ingredients
            .Include(i => i.CategoryMappings)
            .ThenInclude(m => m.Category)
            .ThenInclude(c => c.ConditionRules)
            .Where(i => parsed.LookupCandidates.Contains(i.NormalizedInciName))
            .AsSplitQuery()
            .ToListAsync();
        var knownIngredientLookup = knownIngredients
            .GroupBy(i => IngredientNormalizer.Normalize(i.InciName), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var unknownIngredients = parsed.NormalizedIngredients
            .Where(name => !knownIngredientLookup.ContainsKey(name))
            .ToList();

        // Fallback: some products use INN/Ph. Eur. names instead of INCI (e.g.
        // "Glycerol" instead of "Glycerin"). Only queried when something is
        // still unmatched, since most requests have no unknown ingredients.
        if (unknownIngredients.Count > 0)
        {
            var allSynonyms = await _context.IngredientSynonyms
                .Include(s => s.Ingredient)
                .ThenInclude(i => i.CategoryMappings)
                .ThenInclude(m => m.Category)
                .ThenInclude(c => c.ConditionRules)
                .AsSplitQuery()
                .ToListAsync();
            var synonymLookup = allSynonyms
                .GroupBy(s => IngredientNormalizer.Normalize(s.SynonymName), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().Ingredient, StringComparer.Ordinal);

            foreach (var name in unknownIngredients)
            {
                if (synonymLookup.TryGetValue(name, out var ingredient))
                {
                    knownIngredientLookup[name] = ingredient;
                }
            }

            unknownIngredients = parsed.NormalizedIngredients
                .Where(name => !knownIngredientLookup.ContainsKey(name))
                .ToList();
        }

        return new IngredientResolution(
            parsed.NormalizedIngredients,
            knownIngredientLookup,
            unknownIngredients);
    }

    private async Task<AnalyseResponse> EnrichAsync(
        IngredientResolution resolution,
        HashSet<Condition> selectedConditions)
    {
        var response = new AnalyseResponse();
        var shouldClassifyUnknownIngredients =
            resolution.UnknownIngredients.Count <= MaxUnknownIngredientsForAiClassification;

        foreach (var cleanedName in resolution.NormalizedIngredients)
        {
            if (!resolution.KnownIngredients.TryGetValue(cleanedName, out var ingredient))
            {
                response.UnknownIngredients.Add(shouldClassifyUnknownIngredients
                    ? await _huggingFace.ClassifyAsync(cleanedName)
                    : new AiIngredientResultDto
                    {
                        Name = cleanedName,
                        AiLabel = "Unknown",
                        Confidence = 0
                    });
                continue;
            }

            var confirmedUvMapping = ingredient.CategoryMappings
                .Any(UvFilterClassifier.IsConfirmedAnnexViMapping);
            var relevantMappings = ingredient.CategoryMappings
                .Where(mapping =>
                    mapping.Category.Name != "UV Filter" ||
                    UvFilterClassifier.IsConfirmedAnnexViMapping(mapping))
                .ToList();
            var result = new IngredientResultDto
            {
                InciName = ingredient.InciName,
                SafetyRating = ingredient.SafetyRating.ToString(),
                IsUvFilter = confirmedUvMapping,
                Category = string.Join(", ", relevantMappings
                    .Select(m => m.Category.Name)
                    .OrderBy(name => name)),
                Function = ingredient.Function,
                // Personalisation happens here.
                // If the user did not select any conditions, this returns an
                // empty list. If they selected conditions, only matching rules
                // are shown.
                ConditionFlags = relevantMappings
                    .SelectMany(m => m.Category.ConditionRules)
                    .Where(cr => selectedConditions.Contains(cr.Condition))
                    .GroupBy(cr => cr.Id)
                    .Select(group => group.First())
                    .Select(cr => new ConditionFlagDto
                    {
                        Condition = cr.Condition.ToString(),
                        FlagType = cr.FlagType.ToString(),
                        Notes = cr.Notes,
                        EvidenceSource = cr.EvidenceSource
                    }).ToList()
            };
            result.UvFilterType = result.IsUvFilter
                ? UvFilterClassifier.Classify(ingredient.InciName)
                : string.Empty;
            response.Results.Add(result);
        }

        return response;
    }

    private Task<string> BuildSummaryAsync(
        AnalyseResponse response,
        List<string>? userConditions,
        string? ageGroup,
        string? gender)
    {
        return _aiSummary.SummariseAsync(
            response,
            userConditions ?? new List<string>(),
            ageGroup,
            gender);
    }

    private sealed record ParsedIngredients(
        List<string> NormalizedIngredients,
        List<string> LookupCandidates);

    private sealed record IngredientResolution(
        List<string> NormalizedIngredients,
        Dictionary<string, Ingredient> KnownIngredients,
        List<string> UnknownIngredients);
}
