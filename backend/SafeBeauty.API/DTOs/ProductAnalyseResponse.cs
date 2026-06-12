namespace SafeBeauty.API.DTOs;

public class ProductAnalyseResponse
{
    public string ProductName {get; set;} = string.Empty;
    public string Barcode {get; set;} = string.Empty;
    public AnalyseResponse Analysis {get; set;} = new();
}