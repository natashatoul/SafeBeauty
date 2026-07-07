namespace SafeBeauty.API.DTOs;

public class AnalyseResponse
{
    public List<IngredientResultDto> Results {get; set;} = new();
    public List<AiIngredientResultDto> UnknownIngredients {get; set;} = new();
    public string AiSummary {get; set;} = string.Empty;
}