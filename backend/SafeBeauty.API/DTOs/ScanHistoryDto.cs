using System.Text.Json;

namespace SafeBeauty.API.DTOs;

public class ScanHistoryDto
{
    public string Id { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public JsonElement Results { get; set; }
    public JsonElement AnalysisContext { get; set; }
}
