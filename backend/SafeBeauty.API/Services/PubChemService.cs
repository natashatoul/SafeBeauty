
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

public sealed record PubChemDescription(
    int? Cid,
    string? Title,
    string Description,
    string? SourceName,
    string? SourceUrl
);

public sealed class PubChemService
{
    private readonly HttpClient _httpClient;

    public PubChemService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<PubChemDescription>> GetDescriptionsAsync(
        string inciName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(inciName))
            return new();

        string name = Uri.EscapeDataString(inciName.Trim());

        string url =
            $"compound/name/{name}/description/JSON";

        using var response = await _httpClient.GetAsync(
            url, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return new();

        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<
            PubChemResponse>(cancellationToken);

        var items = data?.InformationList?.Information ?? new();

        string? title = items
            .Select(x => x.Title)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        return items
            .Where(x => !string.IsNullOrWhiteSpace(x.Description))
            .Select(x => new PubChemDescription(
                x.Cid,
                x.Title ?? title,
                x.Description!,
                x.DescriptionSourceName,
                x.DescriptionURL
            ))
            .ToList();
    }

    private sealed class PubChemResponse
    {
        public InformationListDto? InformationList { get; set; }
    }

    private sealed class InformationListDto
    {
        public List<InformationDto> Information { get; set; } = new();
    }

    private sealed class InformationDto
    {
        [JsonPropertyName("CID")]
        public int? Cid { get; set; }

        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? DescriptionSourceName { get; set; }

        [JsonPropertyName("DescriptionURL")]
        public string? DescriptionURL { get; set; }
    }
}
