using SafeBeauty.API.Models.Enums;

namespace SafeBeauty.API.Models;

public class Ingredient
{
    public int Id { get; set; }
    public string InciName { get; set; } = string.Empty;
    public string NormalizedInciName { get; set; } = string.Empty;
    public string? CasNumber { get; set; } = string.Empty;
    public string Function { get; set; } = string.Empty;
    public SafetyRating SafetyRating { get; set; }
    public string Source { get; set; } = string.Empty;

    public ICollection<IngredientCategoryMapping> CategoryMappings { get; set; } = new List<IngredientCategoryMapping>();
    public ICollection<IngredientSynonym> Synonyms { get; set; } = new List<IngredientSynonym>();
    public ICollection<AnnexRestriction> AnnexRestrictions { get; set; } = new List<AnnexRestriction>();
}
