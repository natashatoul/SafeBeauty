namespace SafeBeauty.API.DTOs;

public class ProductAnalyseResponse
{
    public string ProductName {get; set;} = string.Empty;
    public string Barcode {get; set;} = string.Empty;
    public List<string> SourceIngredients {get; set;} = new();
    public AnalyseResponse Analysis {get; set;} = new();
}
