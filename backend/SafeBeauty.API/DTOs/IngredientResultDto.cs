namespace SafeBeauty.API.DTOs;

public class IngredientResultDto
{
    public string InciName {get; set;} = string.Empty;
    public string SafetyRating {get; set;} = string.Empty;
    public string Category {get; set;} = string.Empty;
    public string Function {get; set;} = string.Empty;
    public bool IsUvFilter { get; set; }
    public string UvFilterType { get; set; } = string.Empty;
    public List<ConditionFlagDto> ConditionFlags  {get; set;} = new();
}
