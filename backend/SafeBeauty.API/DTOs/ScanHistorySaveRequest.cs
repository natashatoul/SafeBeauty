using System.Text.Json;

namespace SafeBeauty.API.DTOs;

public class ScanHistorySaveRequest
{
    public JsonElement Results { get; set; }
    public JsonElement AnalysisContext { get; set; }
}
