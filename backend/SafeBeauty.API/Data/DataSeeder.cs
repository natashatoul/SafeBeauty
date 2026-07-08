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
    }

    // 1. Create IngredientCategory records without using ingredient_categories.csv.
    // The old ingredient_categories.csv file is manually created and too small to be
    // reliable as a safety data source. Categories still matter for skin-condition
    // rules, so we derive them from condition_rules.csv and add the regulatory
    // categories needed by the official CosIng annex seeders.
    private async Task<Dictionary<string, IngredientCategory>>
    SeedCategoriesAsync()
    {
        if (await _context.IngredientCategories.AnyAsync())
        {
            return await _context.IngredientCategories
            .ToDictionaryAsync(c => c.Name);
        }
        var filePath = Path.Combine(_dataPath, "condition_rules.csv");
        var lines = await File.ReadAllLinesAsync(filePath);
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

        var categories = new Dictionary<string, IngredientCategory>();

        foreach (var name in categoryNames)
        {
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
        if (await _context.ConditionRules.AnyAsync()) return;
        var filePath = Path.Combine(_dataPath, "condition_rules.csv");
        var lines = await File.ReadAllLinesAsync(filePath);
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

            _context.ConditionRules.Add(new ConditionRule
            {
                CategoryId = category.Id,
                Condition = condition,
                FlagType = flagType,
                EvidenceSource = evidence,
                Notes = notes
            });
        }
        await _context.SaveChangesAsync();
    }

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

        var lines = await File.ReadAllLinesAsync(filePath);
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

            _context.Ingredients.Add(new Ingredient
            {
                InciName = inciName,
                CategoryId = category.Id,
                SafetyRating = SafetyRating.Grey,
                Function = string.Empty,
                Source = source
            });

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

        var lines = await File.ReadAllLinesAsync(filePath);

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
            var existing = await _context.Ingredients.FirstOrDefaultAsync(i => i.InciName == name);
            if (existing != null)
            {
                existing.CategoryId = category.Id;
                existing.SafetyRating = SafetyRating.Amber;
                existing.Function = "Perfuming";
                existing.Source = "OfficialDerived_GenericFragranceTerm";
            }
            else
            {
                _context.Ingredients.Add(new Ingredient
                {
                    InciName = name,
                    CategoryId = category.Id,
                    SafetyRating = SafetyRating.Amber,
                    Function = "Perfuming",
                    Source = "OfficialDerived_GenericFragranceTerm"
                });
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

    private static List<string> SplitInciNames(string inciField)
    {
        return inciField
            .Split(new[] { ",", "/" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(n => n.Trim())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task SeedAnnexIIAsync(Dictionary<string, IngredientCategory> categories)
    {
        if (await _context.Ingredients.AnyAsync(i => i.Source == "COSING_Annex_II")) return;

        var catName = "Prohibited Substance";
        if (!categories.TryGetValue(catName, out var category))
        {
            category = new IngredientCategory { Name = catName };
            _context.IngredientCategories.Add(category);
            await _context.SaveChangesAsync();
            categories[catName] = category;
        }

        var filePath = Path.Combine(_dataPath, "COSING_Annex_II_v2.txt");
        var lines = await File.ReadAllLinesAsync(filePath);

        foreach (var line in lines.Skip(5).Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            var parts = ParseCsvLine(line);
            if (parts.Length < 9) continue;

            var inciName = parts[8].Trim();
            var chemicalName = parts[1].Trim();
            var casNumber = parts[2].Trim();

            var names = SplitInciNames(inciName);
            if (names.Count == 0 && !string.IsNullOrWhiteSpace(chemicalName))
            {
                names.Add(chemicalName);
            }

            foreach (var name in names)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;

                var existing = await _context.Ingredients.FirstOrDefaultAsync(i => i.InciName == name);
                if (existing != null)
                {
                    existing.CategoryId = category.Id;
                    existing.SafetyRating = SafetyRating.Red;
                    existing.Source = "COSING_Annex_II";
                }
                else
                {
                    _context.Ingredients.Add(new Ingredient
                    {
                        InciName = name,
                        CasNumber = string.IsNullOrWhiteSpace(casNumber) ? null : casNumber,
                        CategoryId = category.Id,
                        SafetyRating = SafetyRating.Red,
                        Function = string.Empty,
                        Source = "COSING_Annex_II"
                    });
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
        if (await _context.Ingredients.AnyAsync(i => i.Source == source)) return;

        if (!categories.TryGetValue(categoryName, out var category))
        {
            category = new IngredientCategory { Name = categoryName };
            _context.IngredientCategories.Add(category);
            await _context.SaveChangesAsync();
            categories[categoryName] = category;
        }

        var filePath = Path.Combine(_dataPath, fileName);
        var lines = await File.ReadAllLinesAsync(filePath);

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
                var existing = await _context.Ingredients.FirstOrDefaultAsync(i => i.InciName == name);
                if (existing != null)
                {
                    existing.CategoryId = category.Id;
                    existing.SafetyRating = rating;
                    existing.Source = source;

                    if (!string.IsNullOrWhiteSpace(function))
                    {
                        existing.Function = function;
                    }
                }
                else
                {
                    _context.Ingredients.Add(new Ingredient
                    {
                        InciName = name,
                        CasNumber = string.IsNullOrWhiteSpace(casNumber) ? null : casNumber,
                        CategoryId = category.Id,
                        SafetyRating = rating,
                        Function = function,
                        Source = source
                    });
                }
            }
        }
        await _context.SaveChangesAsync();
    }

}
