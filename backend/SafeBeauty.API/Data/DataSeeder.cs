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
        await SeedIngredientSynonymsAsync();
        await SeedRegulatoryHeadNameSynonymsAsync();


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
            "EU Glossary Ingredient",
            "Humectants",
            "Emollients"
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

        var category = await EnsureCategoryAsync("EU Glossary Ingredient", categories);

        var filePath = Path.Combine(_dataPath, "EU_2025_1175_Glossary_Common_Ingredient_Names.csv");
        if (!File.Exists(filePath)) return;

        var existingNamesList = await _context.Ingredients
            .Select(i => i.NormalizedInciName)
            .ToListAsync();
        var existingNames = new HashSet<string>(existingNamesList, StringComparer.Ordinal);

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
            var normalizedName = IngredientNormalizer.Normalize(inciName);
            if (normalizedName.Length == 0) continue;
            if (!existingNames.Add(normalizedName)) continue;

            var ingredient = new Ingredient
            {
                InciName = inciName,
                NormalizedInciName = normalizedName,
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
        const string source = "COSING_Ingredients_FragranceInventory";

        // Earlier versions only enriched names that had first appeared in an
        // EU glossary or an Annex. CosIng contains valid INCI names that are
        // absent from that local glossary extract, so those ingredients were
        // incorrectly sent to the AI "unknown" fallback.
        if (await _context.Ingredients.AnyAsync(i => i.Source == source))
        {
            Console.WriteLine("CosIng ingredient inventory already seeded, skipping.");
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

        var ingredients = await _context.Ingredients
            .ToDictionaryAsync(i => i.NormalizedInciName, StringComparer.Ordinal);
        var updates = 0;
        var additions = 0;

        foreach (var line in lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            var parts = ParseCsvLine(line);

            if (parts.Length < 9) continue;

            var inciName = parts[1].Trim();
            var normalizedName = IngredientNormalizer.Normalize(inciName);
            var function = parts[8].Trim();

            if (normalizedName.Length == 0) continue;
            if (string.IsNullOrWhiteSpace(function)) continue;

            if (ingredients.TryGetValue(normalizedName, out var existing))
            {
                // Do not overwrite regulatory category/rating here. This only
                // enriches a record with the official CosIng function.
                if (string.IsNullOrWhiteSpace(existing.Function))
                {
                    existing.Function = function;
                    updates++;
                }
                continue;
            }

            var ingredient = new Ingredient
            {
                InciName = inciName,
                NormalizedInciName = normalizedName,
                SafetyRating = SafetyRating.Grey,
                Function = function,
                Source = source
            };
            _context.Ingredients.Add(ingredient);
            ingredients[normalizedName] = ingredient;
            additions++;
        }

        await _context.SaveChangesAsync();

        Console.WriteLine($"CosIng inventory imported: {additions} new, {updates} enriched.");
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
        var category = await EnsureCategoryAsync("Fragrance", categories);

        var genericNames = new[] { "PARFUM", "FRAGRANCE", "AROMA" };

        foreach (var name in genericNames)
        {
            var normalizedName = IngredientNormalizer.Normalize(name);
            var existing = await _context.Ingredients
                .Include(i => i.CategoryMappings)
                .FirstOrDefaultAsync(i => i.NormalizedInciName == normalizedName);
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
                    NormalizedInciName = normalizedName,
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

    // its a markers
    private static readonly string[] HeadNameCutMarkers =
    {
      " and its", ", its", " and their", ", their", " (INN)", ";"
    };

    // Regulatory names in CosIng Annex II/III are often phrased as
    // "Substance and its salts/compounds/derivatives" rather than the
    // plain substance name a real user would type on a label. This takes
    // the text before the earliest such marker as a shorter "common name"
    // candidate, e.g. "Arsenic and its compounds" -> "Arsenic".
    private static string? TryExtractHeadName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return null;

        var earliestIndex = -1;
        foreach (var marker in HeadNameCutMarkers)
        {
            var index = fullName.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index > 0 && (earliestIndex == -1 || index < earliestIndex))
            {
                earliestIndex = index;
            }
        }

        if (earliestIndex <= 0) return null;

        var head = fullName[..earliestIndex].Trim().TrimEnd(',', ';');
        return head.Length >= 3 ? head : null;
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
        var pendingByName = new Dictionary<string, Ingredient>(StringComparer.Ordinal);

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

            foreach (var name in names.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                var normalizedName = IngredientNormalizer.Normalize(name);

                var existing = await FindTrackedIngredientAsync(normalizedName, pendingByName);
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
                        NormalizedInciName = normalizedName,
                        CasNumber = string.IsNullOrWhiteSpace(casNumber) ? null : casNumber,
                        SafetyRating = rating,
                        Function = string.Empty,
                        Source = "COSING_Annex_II"
                    };
                    AddCategoryMapping(ingredient, category, mappingType,
                        "COSING_Annex_II", mappingNotes);
                    _context.Ingredients.Add(ingredient);
                    pendingByName[normalizedName] = ingredient;
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

        var category = await EnsureCategoryAsync(categoryName, categories);

        var filePath = Path.Combine(_dataPath, fileName);
        var lines = await ReadCsvRecordsAsync(filePath);
        var pendingByName = new Dictionary<string, Ingredient>(StringComparer.Ordinal);

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
                var normalizedName = IngredientNormalizer.Normalize(name);
                var existing = await FindTrackedIngredientAsync(normalizedName, pendingByName);
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
                        NormalizedInciName = normalizedName,
                        CasNumber = string.IsNullOrWhiteSpace(casNumber) ? null : casNumber,
                        SafetyRating = rating,
                        Function = function,
                        Source = source
                    };
                    AddCategoryMapping(ingredient, category, mappingType, source,
                        $"Substance is listed in {source}.");
                    _context.Ingredients.Add(ingredient);
                    pendingByName[normalizedName] = ingredient;
                }
            }
        }
        await _context.SaveChangesAsync();
    }

    private async Task<Ingredient?> FindTrackedIngredientAsync(
        string normalizedName,
        Dictionary<string, Ingredient> pendingByName)
    {
        if (pendingByName.TryGetValue(normalizedName, out var pending))
        {
            return pending;
        }

        return await _context.Ingredients
            .Include(i => i.CategoryMappings)
            .FirstOrDefaultAsync(i => i.NormalizedInciName == normalizedName);
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

// Auto-derives ingredient <-> category links for the two broadest CosIng
// functions directly in SQL, instead of listing every ingredient by hand:
// Humectants (2200+) and Emollients (3300+) are far too large to seed from
// a manual CSV. Only these two functions get this bulk treatment; other
// CosIng functions (solvent, chelating, film forming, etc.) are not mapped
// to a category this way and stay as raw Function text only.

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
            .Select(parts => IngredientNormalizer.Normalize(parts[0]))
            .Distinct(StringComparer.Ordinal)
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
            .Where(i => requestedNames.Contains(i.NormalizedInciName))
            .ToListAsync();
        var ingredients = ingredientList
            .GroupBy(i => i.NormalizedInciName, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(),
                StringComparer.Ordinal);

        foreach (var parts in parsedRows)
        {
            var inciName = IngredientNormalizer.Normalize(parts[0]);
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

    // it is a scale of trastworth jf ingredient source
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

    private async Task SeedIngredientSynonymsAsync()
    {
        // Guard: only needs to run once.
        if (await _context.IngredientSynonyms.AnyAsync())
        {
            Console.WriteLine("Ingredient synonyms already seeded, skipping.");
            return;
        }

        var filePath = Path.Combine(_dataPath, "COSING_Ingredients-Fragrance Inventory_v2.csv");
        if (!File.Exists(filePath)) return;

        var lines = await ReadCsvRecordsAsync(filePath);

        // CSV headers: 0 = COSING Ref No, 1 = INCI name, 2 = INN name, 3 = Ph. Eur. Name
        var added = 0;

        foreach (var line in lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            var parts = ParseCsvLine(line);
            if (parts.Length < 4) continue;

            var inciName = parts[1].Trim();
            var normalizedInciName = IngredientNormalizer.Normalize(inciName);
            if (normalizedInciName.Length == 0) continue;

            var ingredient = await _context.Ingredients
                .Include(i => i.Synonyms)
                .FirstOrDefaultAsync(i => i.NormalizedInciName == normalizedInciName);
            if (ingredient == null) continue;

            var candidateSynonyms = new[] { parts[2].Trim(), parts[3].Trim() };

            foreach (var candidate in candidateSynonyms)
            {
                if (string.IsNullOrWhiteSpace(candidate)) continue;

                var normalizedSynonym = IngredientNormalizer.Normalize(candidate);
                if (normalizedSynonym.Length == 0) continue;
                if (normalizedSynonym == normalizedInciName) continue;

                var alreadyExists = ingredient.Synonyms.Any(s =>
                    string.Equals(s.SynonymName, candidate, StringComparison.OrdinalIgnoreCase));
                if (alreadyExists) continue;

                ingredient.Synonyms.Add(new IngredientSynonym { SynonymName = candidate });
                added++;
            }
        }

        await _context.SaveChangesAsync();
        Console.WriteLine($"Ingredient synonyms seeded: {added}");
    }

    private async Task SeedRegulatoryHeadNameSynonymsAsync()
    {
        var allIngredients = await _context.Ingredients
            .Include(i => i.Synonyms)
            .ToListAsync();

        var existingNormalizedNames = allIngredients
            .Select(i => i.NormalizedInciName)
            .ToHashSet(StringComparer.Ordinal);

        // First pass: collect head-name candidates grouped by normalized head,
        // so entries that reduce to the same short name can be detected and
        // skipped instead of silently pointing to the wrong ingredient.
        var candidatesByNormalizedHead = new Dictionary<string, List<(Ingredient Ingredient, string Head)>>(StringComparer.Ordinal);

        foreach (var ingredient in allIngredients)
        {
            // Only official Annex records receive a shortened regulatory alias.
            // Ordinary glossary and manually derived names must stay untouched.
            if (!ingredient.Source.StartsWith("COSING_Annex_", StringComparison.Ordinal))
            {
                continue;
            }

            var head = TryExtractHeadName(ingredient.InciName);
            if (head == null) continue;

            var normalizedHead = IngredientNormalizer.Normalize(head);
            if (normalizedHead.Length == 0) continue;
            if (normalizedHead == ingredient.NormalizedInciName) continue;

            // Do not alias over an ingredient that is already independently
            // known under this exact name — its own data stays authoritative.
            if (existingNormalizedNames.Contains(normalizedHead)) continue;

            if (!candidatesByNormalizedHead.TryGetValue(normalizedHead, out var list))
            {
                list = new List<(Ingredient, string)>();
                candidatesByNormalizedHead[normalizedHead] = list;
            }
            list.Add((ingredient, head));
        }

        var added = 0;
        var skippedAmbiguous = 0;

        foreach (var (normalizedHead, candidates) in candidatesByNormalizedHead)
        {
            // Two different regulatory entries reduced to the same short name —
            // ambiguous, so deliberately add a synonym for neither.
            var distinctIngredients = candidates.Select(c => c.Ingredient.Id).Distinct().Count();
            if (distinctIngredients > 1)
            {
                skippedAmbiguous++;
                continue;
            }

            var (ingredient, head) = candidates[0];

            var alreadyExists = ingredient.Synonyms.Any(s =>
                string.Equals(s.SynonymName, head, StringComparison.OrdinalIgnoreCase));
            if (alreadyExists) continue;

            ingredient.Synonyms.Add(new IngredientSynonym { SynonymName = head });
            added++;
        }

        await _context.SaveChangesAsync();
        Console.WriteLine($"Regulatory head-name synonyms seeded: {added} (skipped {skippedAmbiguous} ambiguous heads).");
    }

}
