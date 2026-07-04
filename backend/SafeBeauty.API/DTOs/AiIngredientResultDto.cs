namespace SafeBeauty.API.DTOs;

public class AiIngredientResultDto
{
    public string Name {get; set; } = string.Empty;
    public string AiLabel {get; set;} = string.Empty;
    public double Confidence {get; set;}
}