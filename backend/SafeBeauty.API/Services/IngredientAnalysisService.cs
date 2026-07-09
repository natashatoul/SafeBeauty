
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

    // Constructor injection - SafeBeautyDbContext is provided by ASP.NET DI container
    public IngredientAnalysisService(SafeBeautyDbContext context, HuggingFaceService huggingFace, AiSummaryService aiSummary)
    {
        _context = context;
        _huggingFace = huggingFace;
        _aiSummary = aiSummary;
    }

    public async Task<AnalyseResponse> AnalyseAsync(List<string> ingredients, List<string>? userConditions = null)
    {
        var response = new AnalyseResponse();

        // Convert frontend condition strings into the backend enum values.
        // Unknown strings are ignored, so an old localStorage value cannot break
        // the whole analysis request.
        var selectedConditions = (userConditions ?? new List<string>())
            .Select(c => Enum.TryParse<Condition>(c, out var condition)
                ? condition
                : (Condition?)null)
            .Where(c => c.HasValue)
            .Select(c => c!.Value)
            .ToHashSet();

        var normalizedIngredients = ingredients
            .Select(IngredientNormalizer.Normalize)
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        foreach (var cleanedName in normalizedIngredients)
        {
            var ingredient = await _context.Ingredients
            .Include(i => i.CategoryMappings)
            .ThenInclude(m => m.Category)
            .ThenInclude(c => c.ConditionRules)
            .FirstOrDefaultAsync(i => i.NormalizedInciName == cleanedName);

            if (ingredient == null)
            {
                var aiResult = await _huggingFace.ClassifyAsync(cleanedName);
                response.UnknownIngredients.Add(aiResult);
                continue;
            }

            var result = new IngredientResultDto
            {
                InciName = ingredient.InciName,
                SafetyRating = ingredient.SafetyRating.ToString(),
                Category = string.Join(", ", ingredient.CategoryMappings
                    .Select(m => m.Category.Name)
                    .OrderBy(name => name)),
                Function = ingredient.Function,
                // Personalisation happens here.
                // If the user did not select any conditions, this returns an
                // empty list. If they selected conditions, only matching rules
                // are shown.
                ConditionFlags = ingredient.CategoryMappings
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
            response.Results.Add(result);
        }
        response.AiSummary = await _aiSummary.SummariseAsync(response, userConditions ?? new List<string>());
        return response;
        
    }
}
