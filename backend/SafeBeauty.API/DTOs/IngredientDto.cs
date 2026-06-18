namespace SafeBeauty.API.DTOs;

public class IngredientDto
{
    public int Id { get; set; }
    public string InciName { get; set; } = string.Empty;
    public string? CasNumber { get; set; }
    public string Function { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string SafetyRating { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public IngredientCategoryDto? Category { get; set; }
}

public class IngredientCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
