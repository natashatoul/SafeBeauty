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
        await SeedIngredientAsync(categories);
        await SeedAnnexIIAsync(categories);
        await SeedAnnexAsync("COSING_Annex_III_v2.txt", "Restricted Substance", SafetyRating.Amber, string.Empty, categories);
        await SeedAnnexAsync("COSING_Annex_IV_v2.txt", "Colorant", SafetyRating.Green, "Colorant", categories);
        await SeedAnnexAsync("COSING_Annex_V_v2.txt", "Preservative", SafetyRating.Green, "Preservative", categories);
        await SeedAnnexAsync("COSING_Annex_VI_v2.txt", "UV Filter", SafetyRating.Green, "UV Filter", categories);
    }

    // 1. Read unique categories from ingredient_categories.csv and create records in IngredientCategories table
    private async Task<Dictionary<string, IngredientCategory>>
    SeedCategoriesAsync()
    {
        if (await _context.IngredientCategories.AnyAsync())
        {
            return await _context.IngredientCategories
            .ToDictionaryAsync(c => c.Name);
        }
        var filePath = Path.Combine(_dataPath, "ingredient_categories.csv");
        var lines = await File.ReadAllLinesAsync(filePath);
        var categoryNames = lines
        .Skip(1) // skip title (inci_name, category)
        .Where(l => !string.IsNullOrWhiteSpace(l))
        .Select(l => l.Split(',')[1].Trim()) // take second column - category name
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
            ["Keratosis Pilaris"] = Condition.KeratosisPilaris
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

    // 3. read ingredient_categories,csv and create records to Ingredient
    private async Task SeedIngredientAsync(Dictionary<string, IngredientCategory> categories)
    {
        if (await _context.Ingredients.AnyAsync(i => i.Source == "ingredient_categories.csv")) return;
        var filePath = Path.Combine(_dataPath, "ingredient_categories.csv");
        var lines = await File.ReadAllLinesAsync(filePath);
        foreach (var line in lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            // ingredient_categoories.csv: inci_name, category
            var parts = line.Split(',');
            if (parts.Length < 2) continue;
            var inciName = parts[0].Trim();
            var categoryStr = parts[1].Trim();
            if (!categories.TryGetValue(categoryStr, out var category)) continue;

            _context.Ingredients.Add(new Ingredient
            {
                InciName = inciName,
                CategoryId = category.Id,
                SafetyRating = SafetyRating.Grey,
                Function = string.Empty,
                Source = "ingredient_categories.csv"
            });
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

            var name = string.IsNullOrWhiteSpace(inciName) ? chemicalName : inciName;
            if (string.IsNullOrWhiteSpace(name)) continue;

            var existing = await _context.Ingredients.FirstOrDefaultAsync(i => i.InciName == name);
            if (existing != null)
            {
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

            var names = inciField.Split(" / ", StringSplitOptions.RemoveEmptyEntries)
                                 .Select(n => n.Trim())
                                 .Where(n => !string.IsNullOrWhiteSpace(n))
                                 .ToList();

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
                    existing.SafetyRating = rating;
                    existing.Source = source;
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
