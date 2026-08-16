// using lines import the tools this file needs from .NET libraries.
using System.Net.Http.Headers;   // AuthenticationHeaderValue (for the Bearer token header)
using System.Text;               // Encoding.UTF8 (to encode the request body)
using System.Text.Json;          // JsonSerializer and JsonDocument (to write and read JSON)
using SafeBeauty.API.DTOs;       // AiIngredientResultDto (the "envelope" we return to the frontend)

namespace SafeBeauty.API.Services;

/// Sends an unknown ingredient name to the Hugging Face Inference API
/// (zero-shot classification model - model didn't learned on safe cosmetic ingredients)
///  and returns the best-matching safety label
/// together with the model's confidence score.
public class HuggingFaceService
{
    // Private fields. "readonly" means they can only be assigned once,
    // in the constructor, and never replaced afterwards.
    // The underscore prefix (_httpClient) is a C# naming convention for private fields.
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<HuggingFaceService> _logger;

    // It is a array with four labels that model can chooses.
    // "static" = one shared array for the whole class, not a copy per object,
    // because the labels are the same for every request.
    private static readonly string[] Labels =
    [
        "safe cosmetic ingredient",
        "skin irritant",
        "potentially harmful",
        "allergen"
    ];

    // Constructor. The service does NOT create its own tools —
    // ASP.NET Core injects them (dependency injection):
    //   - HttpClient comes from builder.Services.AddHttpClient() in Program.cs
    //   - IConfiguration gives access to appsettings.json and environment variables
    public HuggingFaceService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<HuggingFaceService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

    // Reads the token from configuration:
    // locally  -> the "HuggingFace": { "ApiKey": ... } section in appsettings.json
    // on Azure -> the HuggingFace__ApiKey application setting (env variable)
    // The "!" (null-forgiving operator) is a promise to the compiler
    // that this value definitely exists and is not null.
        _apiKey = configuration["HuggingFace:ApiKey"] ?? string.Empty;
    }
    // This is a main work
    // "async" allows the use of "await" inside.
    // Task<AiIngredientResultDto> = a promise of a result that will arrive later,
    // so the server thread is not blocked while we wait for the network.
    public async Task<AiIngredientResultDto> ClassifyAsync(string ingredientName)
    {
        // Anonymous object (new { ... } with no class name):
        // a throwaway container that exists only to be turned into JSON.
        // The $"..." syntax is string interpolation (like template literals in JS):
        // wrapping the name in a sentence helps the model classify it better.
        var requestBody = new
        {
            inputs = $"{ingredientName} is a cosmetic ingredient",
            parameters = new { candidate_labels = Labels }
        };

        // Turn the object into JSON text...
        var json = JsonSerializer.Serialize(requestBody);

        try
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogWarning("Hugging Face API key is missing.");
                return UnknownResult(ingredientName);
            }

            // ...and wrap it as the HTTP request body,
            // marked as UTF-8 encoded JSON content.
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://router.huggingface.co/hf-inference/models/facebook/bart-large-mnli");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

// Attach the API token as an "Authorization: Bearer hf_..." header.
// "Bearer" literally means "the holder of this token is allowed in".
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

    // Send the POST request to the model's endpoint and wait (non-blocking)
    // for the response.
            var response = await _httpClient.SendAsync(request);

    // Read the response body as text (second await in this method).
            var responseJson = await response.Content.ReadAsStringAsync();

    // Graceful degradation: if Hugging Face returned an error
    // (model still loading, rate limit hit, invalid token, etc.),
    // do NOT crash — return an honest "Unknown" result instead,
    // so the frontend can show "could not determine" rather than break.
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Hugging Face returned {StatusCode}: {ResponseBody}",
                    (int)response.StatusCode,
                    responseJson);
                return UnknownResult(ingredientName);
            }

            // Parse the JSON text into a navigable structure.
            // "using var" = dispose of it (free the memory) when the method ends.
            // Expected shape of the response:
            //   { "labels": ["allergen", ...], "scores": [0.87, ...] }
            // Hugging Face returns labels already sorted by score (highest first),
            // so index [0] is always the best match.
            using var doc = JsonDocument.Parse(responseJson);
            if (!TryReadTopResult(doc.RootElement, out var topLabel, out var topScore))
            {
                _logger.LogWarning("Unexpected Hugging Face response: {ResponseBody}", responseJson);
                return UnknownResult(ingredientName);
            }

            // Pack the result into the DTO "envelope" for the frontend.
            // Math.Round(..., 2) keeps two decimal places: 0.8734 -> 0.87.
            return new AiIngredientResultDto
            {
                Name = ingredientName,
                AiLabel = topLabel,
                Confidence = Math.Round(topScore, 2)
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Hugging Face classification failed for {IngredientName}.", ingredientName);
            return UnknownResult(ingredientName);
        }

    }

    private static bool TryReadTopResult(JsonElement element, out string label, out double score)
    {
        label = "Unknown";
        score = 0;

        if (element.ValueKind == JsonValueKind.Array)
        {
            if (element.GetArrayLength() == 0)
            {
                return false;
            }

            return TryReadTopResult(element[0], out label, out score);
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (element.TryGetProperty("labels", out var labels)
            && element.TryGetProperty("scores", out var scores)
            && labels.ValueKind == JsonValueKind.Array
            && scores.ValueKind == JsonValueKind.Array
            && labels.GetArrayLength() > 0
            && scores.GetArrayLength() > 0)
        {
            label = labels[0].GetString() ?? "Unknown";
            score = scores[0].GetDouble();
            return true;
        }

        if (element.TryGetProperty("label", out var labelElement)
            && element.TryGetProperty("score", out var scoreElement)
            && scoreElement.ValueKind == JsonValueKind.Number)
        {
            label = labelElement.GetString() ?? "Unknown";
            score = scoreElement.GetDouble();
            return true;
        }

        return false;
    }

    private static AiIngredientResultDto UnknownResult(string ingredientName)
    {
        return new AiIngredientResultDto
        {
            Name = ingredientName,
            AiLabel = "Unknown",
            Confidence = 0
        };
    }









}
