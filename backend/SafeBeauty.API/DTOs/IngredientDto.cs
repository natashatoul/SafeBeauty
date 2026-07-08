namespace SafeBeauty.API.DTOs;

public class IngredientDto
{
    public int Id { get; set; }
    public string InciName { get; set; } = string.Empty;
    public string? CasNumber { get; set; }
    public string Function { get; set; } = string.Empty;
    public string SafetyRating { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public List<IngredientCategoryDto> Categories { get; set; } = new();
}

public class IngredientCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
