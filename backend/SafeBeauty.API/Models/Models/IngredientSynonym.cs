namespace SafeBeauty.API.Models;

public class IngredientSynonym
{
    public int Id { get; set; }
    public int IngredientId { get; set; }
    public string SynonymName { get; set; } = string.Empty;
    
    public Ingredient Ingredient {get; set;} = null!;
}