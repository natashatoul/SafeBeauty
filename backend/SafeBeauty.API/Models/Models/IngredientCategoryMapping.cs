namespace SafeBeauty.API.Models;

public class IngredientCategoryMapping
{
    public int IngredientId { get; set; }
    public int CategoryId { get; set; }
    public string MappingType { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public Ingredient Ingredient { get; set; } = null!;
    public IngredientCategory Category { get; set; } = null!;
}
