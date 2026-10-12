namespace SafeBeauty.API.DTOs;

public class AiIngredientResultDto
{
    public int InputOrder {get; set;} = -1;
    public string Name {get; set; } = string.Empty;
    public string AiLabel {get; set;} = string.Empty;
    public double Confidence {get; set;}
    public string ClassificationStatus { get; set; } = "Unavailable";
}
