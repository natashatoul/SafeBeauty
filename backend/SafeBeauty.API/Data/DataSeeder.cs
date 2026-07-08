using Microsoft.EntityFrameworkCore;
using SafeBeauty.API.Models;
using SafeBeauty.API.Models.Enums;


namespace SafeBeauty.API.Data;

// This class seeds the database with initial data on the first run.
// Categories are seeded first (no FK), then condition rules and ingredients (with FK).
public class DataSeeder
{
    private readonly SafeBeautyDbContext _context;
    private readonly string _dataPath;

    public DataSeeder(SafeBeautyDbContext context)
    {
        _context = context;
        _dataPath = Path.Combine(AppContext.BaseDirectory, "SeedData");
        // when use dotnet run GetCurrentDirectory() returns project folder
        // ".." redirect to the folder backend/data/
    }

    public async Task SeedAsync()
    {
        var categories = await SeedCategoriesAsync();
        await SeedConditionRulesAsync(categories);
        await RemoveManualIngredientSeedDataAsync();
        await SeedEuGlossaryAsync(categories);
        await SeedCosingIngredientFunctionsAsync();
        await SeedGenericFragranceTermsAsync(categories);
        await SeedAnnexIIAsync(categories);
        await SeedAnnexAsync("COSING_Annex_III_v2.txt", "Restricted Substance", SafetyRating.Amber, string.Empty, categories);
        await SeedAnnexAsync("COSING_Annex_IV_v2.txt", "Colorant", SafetyRating.Green, "Colorant", categories);
        await SeedAnnexAsync("COSING_Annex_V_v2.txt", "Preservative", SafetyRating.Green, "Preservative", categories);
        await SeedAnnexAsync("COSING_Annex_VI_v2.txt", "UV Filter", SafetyRating.Green, "UV Filter", categories);
        await SeedFunctionCategoryMappingsAsync(categories);
        await SeedIngredientCategoryMappingsAsync(categories);
    }

    // 1. Create IngredientCategory records without using ingredient_categories.csv.
    // The old ingredient_categories.csv file is manually created and too small to be
    // reliable as a safety data source. Categories still matter for skin-condition
    // rules, so we derive them from condition_rules.csv and add the regulatory
    // categories needed by the official CosIng annex seeders.
    private async Task<Dictionary<string, IngredientCategory>>
    SeedCategoriesAsync()
    {
        var filePath = Path.Combine(_dataPath, "condition_rules.csv");
        var lines = await ReadCsvRecordsAsync(filePath);
        var categoryNames = lines
        .Skip(1) // skip title (inci_name, category)
        .Where(l => !string.IsNullOrWhiteSpace(l))
        .Select(l => l.Split(',')[1].Trim()) // take second column - category name
        .Concat(new[]
        {
            "Prohibited Substance",
            "Restricted Substance",
            "Colorant",
            "Preservative",
            "UV Filter",
            "Fragrance",
            "EU Glossary Ingredient"
        })
        .Distinct() // remove duplicates
        .ToList();

        var categories = await _context.IngredientCategories
            .ToDictionaryAsync(c => c.Name);

        foreach (var name in categoryNames)
        {
            if (categories.ContainsKey(name)) continue;

            var category = new IngredientCategory { Name = name };
            _context.IngredientCategories.Add(category);
            categories[name] = category; // save referense to object
        }

        await _context.SaveChangesAsync(); // after SaveChanges EF Core fiel Id for each category
        return categories; // dictionary "Fragrance" -> object IngredientCategory with Id
    }

    // 2. This method read condition_rules.csv and create records to ConditionRule

    private async Task SeedConditionRulesAsync(Dictionary<string, IngredientCategory> categories)
    {
        var filePath = Path.Combine(_dataPath, "condition_rules.csv");
        var lines = await ReadCsvRecordsAsync(filePath);
        // mapping from CSV on enam Comdition
        var conditionMap = new Dictionary<string, Condition>
        {
            ["Rosacea"] = Condition.Rosacea,
            ["Atopic Dermatitis"] = Condition.AtopicDermatitis,
            ["Psoriasis"] = Condition.Psoriasis,
            ["Alopecia"] = Condition.Alopecia,
            ["Acne"] = Condition.Acne,
            ["Seborrhoeic Dermatitis"] = Condition.SeborrhoeicDermatitis,
            ["Keratosis Pilaris"] = Condition.KeratosisPilaris,
            ["Actinic Keratoses"] = Condition.ActinicKeratoses
        };

        var desiredRules = new List<ConditionRule>();

        foreach (var line in lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            // condition_rules.csv: condition,ingredient_category,flag_type,evidence_source,notes 
            var parts = line.Split(',');
            if (parts.Length < 5) continue;
            var conditionStr = parts[0].Trim();
            var categoryStr = parts[1].Trim();
            var flagStr = parts[2].Trim();
            var evidence = parts[3].Trim();
            var notes = string.Join(",", parts.Skip(4)).Trim(); // notes can have "," and take all after 4-th column
                                                                // if string is not recognised - skip
            if (!conditionMap.TryGetValue(conditionStr, out var condition)) continue;
            if (!categories.TryGetValue(categoryStr, out var category)) continue;
            if (!Enum.TryParse<FlagType>(flagStr, out var flagType)) continue;

            desiredRules.Add(new ConditionRule
            {
                CategoryId = category.Id,
                Condition = condition,
                FlagType = flagType,
                EvidenceSource = evidence,
                Notes = notes
            });
        }

        var existingRules = await _context.ConditionRules.ToListAsync();
        var existingKeys = existingRules.Select(GetConditionRuleKey).OrderBy(key => key).ToList();
        var desiredKeys = desiredRules.Select(GetConditionRuleKey).OrderBy(key => key).ToList();

        if (existingKeys.SequenceEqual(desiredKeys))
        {
            return;
        }

        // Condition rules are entirely seed-owned reference data. Replacing the
        // set when the CSV changes removes stale category names such as
        // "Preservatives" and "UV Filters" from existing databases.
        _context.ConditionRules.RemoveRange(existingRules);
        await _context.SaveChangesAsync();
        _context.ConditionRules.AddRange(desiredRules);
        await _context.SaveChangesAsync();
    }

    private static string GetConditionRuleKey(ConditionRule rule) =>
        string.Join(
            "\u001F",
            rule.Condition,
            rule.CategoryId,
            rule.FlagType,
            rule.EvidenceSource,
            rule.Notes);

    // 3. Remove ingredients that came only from the old manual ingredient list.
    // This prevents the application from treating the 75-row handmade CSV as an
    // authority. Ingredients should now come from official CosIng annex files,
    // explicitly documented derived rules, or the unknown-ingredient AI estimate.
    private async Task RemoveManualIngredientSeedDataAsync()
    {
        var manualIngredients = await _context.Ingredients
            .Where(i => i.Source == "ingredient_categories.csv")
            .ToListAsync();

        if (manualIngredients.Count == 0) return;

        _context.Ingredients.RemoveRange(manualIngredients);
        await _context.SaveChangesAsync();
    }

    private async Task SeedEuGlossaryAsync(Dictionary<string, IngredientCategory> categories)
    {
        var source = "EU_Glossary_2025_1175";
        if (await _context.Ingredients.AnyAsync(i => i.Source == source)) return;

        var catName = "EU Glossary Ingredient";
        if (!categories.TryGetValue(catName, out var category))
        {
            category = new IngredientCategory { Name = catName };
            _context.IngredientCategories.Add(category);
            await _context.SaveChangesAsync();
            categories[catName] = category;
        }

        var filePath = Path.Combine(_dataPath, "EU_2025_1175_Glossary_Common_Ingredient_Names.csv");
        if (!File.Exists(filePath)) return;

        var existingNamesList = await _context.Ingredients
            .Select(i => i.InciName)
            .ToListAsync();
        var existingNames = new HashSet<string>(existingNamesList, StringComparer.OrdinalIgnoreCase);

        var lines = await ReadCsvRecordsAsync(filePath);
        var pendingCount = 0;

        foreach (var line in lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            var parts = ParseCsvLine(line);
            if (parts.Length < 3) continue;

            // The glossary file has both the original INCI name and a normalised
            // version. We store the normalised value because user input is also
            // normalised before lookup, so exact matching becomes more reliable.
            var inciName = parts[2].Trim();
            if (string.IsNullOrWhiteSpace(inciName)) continue;
            if (!existingNames.Add(inciName)) continue;

            var ingredient = new Ingredient
            {
                InciName = inciName,
                SafetyRating = SafetyRating.Grey,
                Function = string.Empty,
                Source = source
            };
            AddCategoryMapping(ingredient, category, "RegulatoryGlossary", source,
                "Ingredient is listed in the EU common ingredient names glossary.");
            _context.Ingredients.Add(ingredient);

            pendingCount++;
            if (pendingCount >= 500)
            {
                await _context.SaveChangesAsync();
                pendingCount = 0;
            }
        }

        if (pendingCount > 0)
        {
            await _context.SaveChangesAsync();
        }
    }

    private async Task SeedCosingIngredientFunctionsAsync()
    {
        // Guard: this enrichment only needs to run once. If any ingredient already
        // has a non-empty Function, the CosIng function data has already been applied
        // on a previous startup, so we skip the whole file to keep startup fast.
        if (await _context.Ingredients.AnyAsync(i => i.Function != string.Empty))
        {
            Console.WriteLine("CosIng ingredient functions already seeded, skipping.");
            return;
        }

        var filePath = Path.Combine(_dataPath, "COSING_Ingredients-Fragrance Inventory_v2.csv");

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"CosIng inventory file not found: {filePath}");
            return;
        }

        var lines = await ReadCsvRecordsAsync(filePath);

        // CSV headers:
        // 0 = COSING Ref No
        // 1 = INCI name
        // 8 = Function

        var updates = 0;

        foreach (var line in lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            var parts = ParseCsvLine(line);

            if (parts.Length < 9) continue;

            var inciName = parts[1].Trim();
            var function = parts[8].Trim();

            if (string.IsNullOrWhiteSpace(inciName)) continue;
            if (string.IsNullOrWhiteSpace(function)) continue;

            var existing = await _context.Ingredients
                .FirstOrDefaultAsync(i => i.InciName == inciName);

            if (existing == null) continue;

            // Do not overwrite regulatory category/rating here.
            // This method only enriches ingredients with real CosIng function data.
            existing.Function = function;
            updates++;
        }

        await _context.SaveChangesAsync();

        Console.WriteLine($"CosIng ingredient functions updated: {updates}");
    }

   
    private async Task SeedGenericFragranceTermsAsync(Dictionary<string, IngredientCategory> categories)
    {
        // PARFUM / FRAGRANCE / AROMA are generic INCI declarations for fragrance
        // compositions, not single molecules. They normally do not appear as one
        // specific restricted substance in Annex III, but they still need cautious
        // handling because the exact fragrance components are not disclosed by the
        // generic label term.
        //
        // This rule is derived from official EU labelling logic: specific fragrance
        // allergens are analysed individually when listed, while generic fragrance
        // terms are treated as a cautionary fragrance mixture.
        var catName = "Fragrance";
        if (!categories.TryGetValue(catName, out var category))
        {
            category = new IngredientCategory { Name = catName };
            _context.IngredientCategories.Add(category);
            await _context.SaveChangesAsync();
            categories[catName] = category;
        }

        var genericNames = new[] { "PARFUM", "FRAGRANCE", "AROMA" };

        foreach (var name in genericNames)
        {
            var existing = await _context.Ingredients
                .Include(i => i.CategoryMappings)
                .FirstOrDefaultAsync(i => i.InciName == name);
            if (existing != null)
            {
                existing.SafetyRating = SafetyRating.Amber;
                existing.Function = "Perfuming";
                existing.Source = "OfficialDerived_GenericFragranceTerm";
                AddCategoryMapping(existing, category, "OfficialDerived",
                    "OfficialDerived_GenericFragranceTerm",
                    "Generic INCI declaration for a fragrance composition.");
            }
            else
            {
                var ingredient = new Ingredient
                {
                    InciName = name,
                    SafetyRating = SafetyRating.Amber,
                    Function = "Perfuming",
                    Source = "OfficialDerived_GenericFragranceTerm"
                };
                AddCategoryMapping(ingredient, category, "OfficialDerived",
                    "OfficialDerived_GenericFragranceTerm",
                    "Generic INCI declaration for a fragrance composition.");
                _context.Ingredients.Add(ingredient);
            }
        }

        await _context.SaveChangesAsync();
    }

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;

        foreach (var ch in line)
        {
            if (ch == '"') { inQuotes = !inQuotes; }
            else if (ch == ',' && !inQuotes) { result.Add(current.ToString()); current.Clear(); }
            else { current.Append(ch); }
        }
        result.Add(current.ToString());
        return result.ToArray();
    }

    private static async Task<List<string>> ReadCsvRecordsAsync(string filePath)
    {
        var records = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        using var reader = new StreamReader(filePath);
        while (await reader.ReadLineAsync() is { } line)
        {
            if (current.Length > 0)
            {
                current.Append('\n');
            }
            current.Append(line);

            for (var index = 0; index < line.Length; index++)
            {
                if (line[index] != '"') continue;

                // Two adjacent quote characters represent an escaped quote
                // inside a quoted CSV value and do not open/close the field.
                if (index + 1 < line.Length && line[index + 1] == '"')
                {
                    index++;
                    continue;
                }

                inQuotes = !inQuotes;
            }

            if (inQuotes) continue;

            records.Add(current.ToString());
            current.Clear();
        }

        if (current.Length > 0)
        {
            records.Add(current.ToString());
        }

        return records;
    }

    private static List<string> SplitInciNames(string inciField)
    {
        return inciField
            .Split(new[] { ",", "/", ";" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(n => n.Trim())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task SeedAnnexIIAsync(Dictionary<string, IngredientCategory> categories)
    {
        const string mappingType = "RegulatoryAnnexNormalizedV3";
        if (await _context.IngredientCategoryMappings
            .AnyAsync(m =>
                m.Source == "COSING_Annex_II" &&
                m.MappingType == mappingType)) return;

        var prohibitedCategory = await EnsureCategoryAsync("Prohibited Substance", categories);
        var restrictedCategory = await EnsureCategoryAsync("Restricted Substance", categories);

        var filePath = Path.Combine(_dataPath, "COSING_Annex_II_v2.txt");
        var lines = await ReadCsvRecordsAsync(filePath);

        foreach (var line in lines.Skip(5).Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            var parts = ParseCsvLine(line);
            if (parts.Length < 9) continue;

            var inciName = parts[8].Trim();
            var chemicalName = parts[1].Trim();
            var casNumber = parts[2].Trim();

            // Some Annex II entries contain an explicit exception such as
            // "except if the full refining history is known". A label INCI name
            // alone cannot prove whether that exception is satisfied, so these
            // entries must be shown as restricted/caution rather than as an
            // unconditional prohibition.
            var hasConditionalException = chemicalName.Contains(
                "except if", StringComparison.OrdinalIgnoreCase);
            var category = hasConditionalException
                ? restrictedCategory
                : prohibitedCategory;
            var rating = hasConditionalException
                ? SafetyRating.Amber
                : SafetyRating.Red;

            var mappingNotes = hasConditionalException
                ? $"Annex II entry contains a conditional exception: {chemicalName}"
                : "Substance is listed in CosIng Annex II.";

            var names = SplitInciNames(inciName);
            if (names.Count == 0 && !string.IsNullOrWhiteSpace(chemicalName))
            {
                names.Add(chemicalName);
            }

            foreach (var name in names)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                var normalizedName = name.ToUpperInvariant();

                var existing = await _context.Ingredients
                    .Include(i => i.CategoryMappings)
                    .FirstOrDefaultAsync(i => i.InciName.ToUpper() == normalizedName);
                if (existing != null)
                {
                    if (hasConditionalException)
                    {
                        var obsoleteMappings = existing.CategoryMappings
                            .Where(m =>
                                m.CategoryId == prohibitedCategory.Id &&
                                (m.MappingType == "LegacyCategory" ||
                                 m.Source == "COSING_Annex_II"))
                            .ToList();
                        _context.IngredientCategoryMappings.RemoveRange(obsoleteMappings);

                        var hasOtherProhibitedMapping = existing.CategoryMappings
                            .Except(obsoleteMappings)
                            .Any(m => m.CategoryId == prohibitedCategory.Id);
                        if (!hasOtherProhibitedMapping &&
                            existing.SafetyRating == SafetyRating.Red)
                        {
                            existing.SafetyRating = SafetyRating.Amber;
                            existing.Source = "COSING_Annex_II";
                        }
                    }

                    ApplyMoreRestrictiveRating(
                        existing, rating, "COSING_Annex_II");
                    AddCategoryMapping(existing, category, mappingType,
                        "COSING_Annex_II", mappingNotes);
                }
                else
                {
                    var ingredient = new Ingredient
                    {
                        InciName = name,
                        CasNumber = string.IsNullOrWhiteSpace(casNumber) ? null : casNumber,
                        SafetyRating = rating,
                        Function = string.Empty,
                        Source = "COSING_Annex_II"
                    };
                    AddCategoryMapping(ingredient, category, mappingType,
                        "COSING_Annex_II", mappingNotes);
                    _context.Ingredients.Add(ingredient);
                }
            }
        }
        await _context.SaveChangesAsync();
    }
    private async Task SeedAnnexAsync(
        string fileName,
        string categoryName,
        SafetyRating rating,
        string function,
        Dictionary<string, IngredientCategory> categories)
    {
        var source = Path.GetFileNameWithoutExtension(fileName);
        const string mappingType = "RegulatoryAnnexNormalizedV3";
        if (await _context.IngredientCategoryMappings
            .AnyAsync(m =>
                m.Source == source &&
                m.MappingType == mappingType)) return;

        if (!categories.TryGetValue(categoryName, out var category))
        {
            category = new IngredientCategory { Name = categoryName };
            _context.IngredientCategories.Add(category);
            await _context.SaveChangesAsync();
            categories[categoryName] = category;
        }

        var filePath = Path.Combine(_dataPath, fileName);
        var lines = await ReadCsvRecordsAsync(filePath);

        foreach (var line in lines.Skip(5).Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            var parts = ParseCsvLine(line);
            if (parts.Length < 3) continue;

            var inciField = parts[2].Trim();
            var chemicalName = parts[1].Trim();
            var casNumber = parts.Length > 3 ? parts[3].Trim() : string.Empty;

            var names = SplitInciNames(inciField);

            if (names.Count == 0)
            {
                if (!string.IsNullOrWhiteSpace(chemicalName)) names.Add(chemicalName);
                else continue;
            }

            foreach (var name in names)
            {
                var normalizedName = name.ToUpperInvariant();
                var existing = await _context.Ingredients
                    .Include(i => i.CategoryMappings)
                    .FirstOrDefaultAsync(i => i.InciName.ToUpper() == normalizedName);
                if (existing != null)
                {
                    ApplyMoreRestrictiveRating(existing, rating, source);
                    AddCategoryMapping(existing, category, mappingType, source,
                        $"Substance is listed in {source}.");

                    if (!string.IsNullOrWhiteSpace(function))
                    {
                        existing.Function = function;
                    }
                }
                else
                {
                    var ingredient = new Ingredient
                    {
                        InciName = name,
                        CasNumber = string.IsNullOrWhiteSpace(casNumber) ? null : casNumber,
                        SafetyRating = rating,
                        Function = function,
                        Source = source
                    };
                    AddCategoryMapping(ingredient, category, mappingType, source,
                        $"Substance is listed in {source}.");
                    _context.Ingredients.Add(ingredient);
                }
            }
        }
        await _context.SaveChangesAsync();
    }

    private async Task<IngredientCategory> EnsureCategoryAsync(
        string categoryName,
        Dictionary<string, IngredientCategory> categories)
    {
        if (categories.TryGetValue(categoryName, out var category))
        {
            return category;
        }

        category = new IngredientCategory { Name = categoryName };
        _context.IngredientCategories.Add(category);
        await _context.SaveChangesAsync();
        categories[categoryName] = category;
        return category;
    }

    private static void ApplyMoreRestrictiveRating(
        Ingredient ingredient,
        SafetyRating candidate,
        string source)
    {
        if (GetSafetySeverity(candidate) <= GetSafetySeverity(ingredient.SafetyRating))
        {
            return;
        }

        ingredient.SafetyRating = candidate;
        ingredient.Source = source;
    }

    private static int GetSafetySeverity(SafetyRating rating) => rating switch
    {
        SafetyRating.Red => 3,
        SafetyRating.Amber => 2,
        SafetyRating.PermittedWithConditions => 2,
        SafetyRating.Green => 1,
        _ => 0
    };

    private async Task SeedFunctionCategoryMappingsAsync(
        Dictionary<string, IngredientCategory> categories)
    {
        var functionMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["HUMECTANT"] = "Humectants",
            ["EMOLLIENT"] = "Emollients"
        };

        foreach (var (functionName, categoryName) in functionMappings)
        {
            if (!categories.TryGetValue(categoryName, out var category)) continue;

            var functionPattern = $"%{functionName}%";
            var source = $"CosIng Function contains {functionName}";
            const string notes =
                "Category is derived directly from the official CosIng function.";

            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT OR IGNORE INTO IngredientCategoryMappings
                    (IngredientId, CategoryId, MappingType, Source, Notes)
                SELECT
                    Id,
                    {category.Id},
                    'CosIngFunction',
                    {source},
                    {notes}
                FROM Ingredients
                WHERE UPPER(Function) LIKE {functionPattern};
                """);
        }
    }

    private async Task SeedIngredientCategoryMappingsAsync(
        Dictionary<string, IngredientCategory> categories)
    {
        var filePath = Path.Combine(_dataPath, "ingredient_category_mappings.csv");
        if (!File.Exists(filePath)) return;

        var lines = await File.ReadAllLinesAsync(filePath);
        var parsedRows = lines.Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(ParseCsvLine)
            .Where(parts => parts.Length >= 5)
            .ToList();
        var requestedNames = parsedRows
            .Select(parts => parts[0].Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var desiredPairs = parsedRows
            .Select(parts => $"{parts[0].Trim()}\u001F{parts[1].Trim()}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var existingManualMappings = await _context.IngredientCategoryMappings
            .Include(mapping => mapping.Ingredient)
            .Include(mapping => mapping.Category)
            .Where(mapping =>
                mapping.MappingType == "ManualDerived" ||
                mapping.MappingType == "ExactIngredient")
            .ToListAsync();
        var obsoleteManualMappings = existingManualMappings
            .Where(mapping => !desiredPairs.Contains(
                $"{mapping.Ingredient.InciName}\u001F{mapping.Category.Name}"))
            .ToList();
        if (obsoleteManualMappings.Count > 0)
        {
            _context.IngredientCategoryMappings.RemoveRange(obsoleteManualMappings);
            await _context.SaveChangesAsync();
        }

        var ingredientList = await _context.Ingredients
            .Include(i => i.CategoryMappings)
            .Where(i => requestedNames.Contains(i.InciName))
            .ToListAsync();
        var ingredients = ingredientList
            .GroupBy(i => i.InciName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(),
                StringComparer.OrdinalIgnoreCase);

        foreach (var parts in parsedRows)
        {
            var inciName = parts[0].Trim();
            var categoryName = parts[1].Trim();
            if (!ingredients.TryGetValue(inciName, out var ingredient)) continue;
            if (!categories.TryGetValue(categoryName, out var category)) continue;

            AddCategoryMapping(
                ingredient,
                category,
                parts[2].Trim(),
                parts[3].Trim(),
                parts[4].Trim());
        }

        await _context.SaveChangesAsync();
    }

    private static void AddCategoryMapping(
        Ingredient ingredient,
        IngredientCategory category,
        string mappingType,
        string source,
        string notes)
    {
        var existing = ingredient.CategoryMappings.FirstOrDefault(m =>
            m.CategoryId == category.Id || ReferenceEquals(m.Category, category));
        if (existing != null)
        {
            if (GetMappingPriority(mappingType) > GetMappingPriority(existing.MappingType))
            {
                existing.MappingType = mappingType;
                existing.Source = source;
                existing.Notes = notes;
            }
            return;
        }

        ingredient.CategoryMappings.Add(new IngredientCategoryMapping
        {
            Category = category,
            MappingType = mappingType,
            Source = source,
            Notes = notes
        });
    }

    private static int GetMappingPriority(string mappingType) => mappingType switch
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
