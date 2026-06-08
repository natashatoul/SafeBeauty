using SafeBeauty.API.Models.Enums;

namespace SafeBeauty.API.Models;

public class AnnexRestriction
{
    public int Id { get; set; }
    public int IngredientId { get; set; }
    public AnnexType AnnexType {get; set;} 
    public string? MaxConcentration { get; set; }
    public string? ProductType { get; set; }
    public string Detail { get; set; } = string.Empty;

    public Ingredient Ingredient { get; set; } = null!;
}