using SafeBeauty.API.Models.Enums;

namespace SafeBeauty.API.Models;

public class Ingredient
{
    public int Id { get; set; }
    public string InciName { get; set; } = string.Empty;
    public string? CasNumber { get; set; } = string.Empty;
    public string Function { get; set; } = string.Empty;
    public int IngredientCategoryID { get; set; }
    public SafetyRating SafetyRating {get; set;} 
    public string Source { get; set; } = string.Empty;

    public IngredientCategory Category {get; set;} = null!;
    public ICollection<IngredientSynonym> Synonims {get; set;} = new List<IngredientSynonym>();
    public ICollection<AnnexRestriction> AnnexRestrictions {get; set;} = new List<AnnexRestriction>();

}