using SafeBeauty.API.Models;
using SafeBeauty.API.Models.Enums;


namespace SafeBeauty.API.Data;

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
        if (_context.IngredientCategories.Any()) return; // if categories already exist
                                                         // seeding was completed, quit
        var categories = await SeedCategoriesAsync();
        await SeedConditionRulesAsync(categories);
        await SeedIngredientAsync(categories);
        // the order is important. First categories without FK, next with FK
    }

    // 1. Read unique categories from ingredient_categories.csv and create records in IngredientCategories table
    private async Task<Dictionary<string, IngredientCategory>>
    SeedCategoriesAsync()
    {
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

    // 2. read condition_rules.csv and create records to ConditionRule

    private async Task SeedConditionRulesAsync(Dictionary<string, IngredientCategory> categories)
    {
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

}