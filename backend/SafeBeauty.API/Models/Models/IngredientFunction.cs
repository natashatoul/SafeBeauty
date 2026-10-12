namespace SafeBeauty.API.Models;

public class IngredientFunction
{
    public int IngredientId { get; set; }
    public Ingredient Ingredient { get; set; } = null!;

    public int CosmeticFunctionId { get; set; }
    public CosmeticFunction CosmeticFunction { get; set; } = null!;
}
