namespace SafeBeauty.API.DTOs;

public class BarcodeAnalyseRequest
{
    public List<string> UserConditions { get; set; } = new();
    public string? AgeGroup { get; set; }
    public string? Gender { get; set; }
}
