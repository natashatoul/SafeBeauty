namespace SafeBeauty.API.DTOs;

public class AnalyseResponse
{
    public List<IngredientResultDto> Results {get; set;} = new();
    public List<string> UnknownIngredients {get; set;} = new(); 
}