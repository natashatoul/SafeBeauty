namespace SafeBeauty.API.Models;

public class IngredientCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();
    public ICollection<ConditionRule> ConditionRules { get; set; } = new List<ConditionRule>();
}