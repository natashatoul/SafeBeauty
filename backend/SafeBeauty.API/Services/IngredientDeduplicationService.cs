using Microsoft.EntityFrameworkCore;
using SafeBeauty.API.Data;
using SafeBeauty.API.Models;
using SafeBeauty.API.Models.Enums;

namespace SafeBeauty.API.Services;

// One-off cleanup service: fills the normalized name column for existing rows,
// then merges case-insensitive duplicates (e.g. "RETINOL" and "Retinol") into a single record.
public class IngredientDeduplicationService
{
    // Dependency injection: the service does not build the database itself,
    // it receives it ready-made (like a cook who is given a kitchen, not one who builds it).
    private readonly SafeBeautyDbContext _context;
    private readonly ILogger<IngredientDeduplicationService> _logger;

    public IngredientDeduplicationService(
        SafeBeautyDbContext context,
        ILogger<IngredientDeduplicationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Entry point: run the two cleanup steps in order.
    // Fill the normalized names first, THEN merge duplicates (merging relies on that column).
    public async Task RunAsync()
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            await FillNormalizedNamesAsync();
            await MergeDuplicatesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task FillNormalizedNamesAsync()
    {
        // Find only the rows that still have an empty normalized name (not yet processed).
        var ingredientsWithoutNormalizedName = await _context.Ingredients
            .Where(i => i.NormalizedInciName == string.Empty)
            .ToListAsync();

        // Nothing to do — leave early.
        if (ingredientsWithoutNormalizedName.Count == 0)
        {
            return;
        }

        // Compute and store the normalized (trimmed + uppercase) name for each row.
        foreach (var ingredient in ingredientsWithoutNormalizedName)
        {
            ingredient.NormalizedInciName = IngredientNormalizer.Normalize(ingredient.InciName);
        }

        // SaveChangesAsync() writes all the pending changes to the database in one go.
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Filled NormalizedInciName for {Count} ingredients",
            ingredientsWithoutNormalizedName.Count);
    }

    private async Task MergeDuplicatesAsync()
    {
        // Load ingredients together with their child records.
        // Include(...) tells EF to also fetch the related rows; without it these
        // collections would be empty in memory and we would lose them when merging.
        var duplicateNames = await _context.Ingredients
            .Where(i => i.NormalizedInciName != string.Empty)
            .GroupBy(i => i.NormalizedInciName)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToListAsync();

        if (duplicateNames.Count == 0)
        {
            _logger.LogInformation("No duplicate ingredients found");
            return;
        }

        var duplicateIngredients = await _context.Ingredients
            .Where(i => duplicateNames.Contains(i.NormalizedInciName))
            .Include(i => i.CategoryMappings)
            .Include(i => i.Synonyms)
            .Include(i => i.AnnexRestrictions)
            .AsSplitQuery()
            .ToListAsync();

        foreach (var group in duplicateIngredients
                     .GroupBy(i => i.NormalizedInciName, StringComparer.Ordinal))
        {
            // The "survivor" is the oldest row (smallest Id); the rest are duplicates to remove.
            var ordered = group.OrderBy(i => i.Id).ToList();
            var survivor = ordered.First();
            var duplicates = ordered.Skip(1).ToList();
            survivor.InciName = survivor.NormalizedInciName;

            foreach (var duplicate in duplicates)
            {
                // Move the duplicate's child records onto the survivor, then delete the duplicate.
                MergeIntoSurvivor(survivor, duplicate);
                _context.Ingredients.Remove(duplicate);

                _logger.LogInformation(
                    "Merged duplicate ingredient '{DuplicateName}' (Id {DuplicateId}) into '{SurvivorName}' (Id {SurvivorId})",
                    duplicate.InciName, duplicate.Id, survivor.InciName, survivor.Id);
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Deduplication complete: processed {GroupCount} duplicate groups",
            duplicateNames.Count);
    }

    // Moves all child records from the duplicate onto the survivor,
    // keeps the strictest safety rating, and fills any empty fields on the survivor.
    private void MergeIntoSurvivor(Ingredient survivor, Ingredient duplicate)
    {
        foreach (var duplicateMapping in duplicate.CategoryMappings.ToList())
        {
            var survivorMapping = survivor.CategoryMappings.FirstOrDefault(
                mapping => mapping.CategoryId == duplicateMapping.CategoryId);

            if (survivorMapping == null)
            {
                // IngredientId is part of the composite primary key. Create a
                // replacement row instead of modifying the tracked key.
                survivor.CategoryMappings.Add(new IngredientCategoryMapping
                {
                    CategoryId = duplicateMapping.CategoryId,
                    MappingType = duplicateMapping.MappingType,
                    Source = duplicateMapping.Source,
                    Notes = duplicateMapping.Notes
                });
            }
            else if (GetMappingPriority(duplicateMapping.MappingType) >
                     GetMappingPriority(survivorMapping.MappingType))
            {
                survivorMapping.MappingType = duplicateMapping.MappingType;
                survivorMapping.Source = duplicateMapping.Source;
                survivorMapping.Notes = duplicateMapping.Notes;
            }

            _context.IngredientCategoryMappings.Remove(duplicateMapping);
        }

        foreach (var synonym in duplicate.Synonyms.ToList())
        {
            var alreadyExists = survivor.Synonyms
                .Any(existing => string.Equals(
                    existing.SynonymName.Trim(),
                    synonym.SynonymName.Trim(),
                    StringComparison.OrdinalIgnoreCase));

            if (alreadyExists)
            {
                _context.IngredientSynonyms.Remove(synonym);
                continue;
            }

            synonym.IngredientId = survivor.Id;
            survivor.Synonyms.Add(synonym);
        }

        // Do not deduplicate only by AnnexType. One Annex may contain several
        // distinct concentration, product-type, or warning restrictions.
        foreach (var restriction in duplicate.AnnexRestrictions.ToList())
        {
            restriction.IngredientId = survivor.Id;
            survivor.AnnexRestrictions.Add(restriction);
        }

        if (GetSafetySeverity(duplicate.SafetyRating) >
            GetSafetySeverity(survivor.SafetyRating))
        {
            survivor.SafetyRating = duplicate.SafetyRating;
            survivor.Source = duplicate.Source;
        }

        if (string.IsNullOrWhiteSpace(survivor.CasNumber) &&
            !string.IsNullOrWhiteSpace(duplicate.CasNumber))
        {
            survivor.CasNumber = duplicate.CasNumber;
        }

        if (string.IsNullOrWhiteSpace(survivor.Function) &&
            !string.IsNullOrWhiteSpace(duplicate.Function))
        {
            survivor.Function = duplicate.Function;
        }

        if (string.IsNullOrWhiteSpace(survivor.Source) &&
            !string.IsNullOrWhiteSpace(duplicate.Source))
        {
            survivor.Source = duplicate.Source;
        }
    }

    private static int GetSafetySeverity(SafetyRating rating) => rating switch
    {
        SafetyRating.Red => 3,
        SafetyRating.Amber => 2,
        SafetyRating.PermittedWithConditions => 2,
        SafetyRating.Green => 1,
        _ => 0
    };

    private static int GetMappingPriority(string mappingType) =>
        mappingType switch
        {
            "RegulatoryAnnexNormalizedV3" => 7,
            "RegulatoryAnnexMultilineV2" => 6,
            "RegulatoryAnnexParsed" => 5,
            "RegulatoryAnnex" => 4,
            "RegulatoryGlossary" => 3,
            "OfficialDerived" => 3,
            "CosIngFunction" => 2,
            "ExactIngredient" => 2,
            "ManualDerived" => 1,
            _ => 0
        };
}
