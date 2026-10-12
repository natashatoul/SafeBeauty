namespace SafeBeauty.API.Models;

public class CosmeticFunction
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool SupportsSkin { get; set; }
    public bool SupportsFormula { get; set; }

    public ICollection<IngredientFunction> IngredientMappings { get; set; } = new List<IngredientFunction>();
}
