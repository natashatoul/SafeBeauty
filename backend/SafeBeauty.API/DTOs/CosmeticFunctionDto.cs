namespace SafeBeauty.API.DTOs;

public class CosmeticFunctionDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool SupportsSkin { get; set; }
    public bool SupportsFormula { get; set; }
}
