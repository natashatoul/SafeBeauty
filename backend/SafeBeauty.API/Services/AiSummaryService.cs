using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SafeBeauty.API.DTOs;

namespace SafeBeauty.API.Services;

public class AiSummaryService
{
    // HttpClient is the .NET object that sends HTTP requests.
    // We do not create it with "new HttpClient()" here; ASP.NET Core injects it
    // through dependency injection when Program.cs uses AddHttpClient<AiSummaryService>().
    private readonly HttpClient _httpClient;

    // Hugging Face LLM token from configuration.
    // Locally it comes from "HuggingFace": { "LlmApiKey": "..." } in appsettings.Development.json
    // or from an environment variable named HuggingFace__LlmApiKey.
    private readonly string _apiKey;

    // Logger lets us record what went wrong without crashing the application.
    // The user still sees the fallback summary, while we can inspect the server logs.
    private readonly ILogger<AiSummaryService> _logger;

    // Hugging Face's OpenAI-compatible chat endpoint.
    // This endpoint expects "messages" with roles like system/user/assistant.
    private const string ChatUrl = "https://router.huggingface.co/v1/chat/completions";

    // The model that writes the product summary.
    // If this model is unavailable for your token/provider later, this is the
    // one string we can swap without changing the rest of the service.
    private const string ModelId = "meta-llama/Llama-3.1-8B-Instruct";

    public AiSummaryService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AiSummaryService> logger)
    {
        _httpClient = httpClient;
        _apiKey = configuration["HuggingFace:LlmApiKey"] ?? string.Empty;
        _logger = logger;
    }

    public async Task<string> SummariseAsync(AnalyseResponse results, List<string> userConditions)
    {
        // Always build a deterministic fallback first.
        // This is our safety net: if Hugging Face is slow, unavailable, unauthorised,
        // or returns a format we do not understand, the app still shows a useful summary.
        var fallback = BuildFallbackSummary(results);

        // If the token is missing, there is no point making a network request.
        // Returning fallback here avoids a predictable 401 Unauthorized error.
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("No Hugging Face LLM API key found; using fallback summary.");
            return fallback;
        }

        try
        {
            // BuildMessages returns two strings:
            //   systemMessage = rules for the model ("do not invent facts")
            //   userMessage   = the actual analysis data for this product
            var (systemMessage, userMessage) = BuildMessages(results, userConditions);

            // Anonymous object: a temporary C# object that matches the JSON shape
            // expected by the Hugging Face chat endpoint.
            var requestBody = new
            {
                model = ModelId,
                messages = new[]
                {
                    // "system" is the instruction layer: it tells the model how to behave.
                    new { role = "system", content = systemMessage },

                    // "user" is the data layer: it contains this product's analysis.
                    new { role = "user", content = userMessage }
                },

                // Longer than the early MVP summary because the UI now shows
                // the insight as a structured product overview rather than a
                // single short paragraph.
                max_tokens = 320,

                // Low temperature makes the model less creative and more predictable.
                // For safety-related explanations, we want cautious wording, not imagination.
                temperature = 0.3
            };

            // Convert the C# object into JSON text before sending it over HTTP.
            var json = JsonSerializer.Serialize(requestBody);

            // Create one HTTP POST request to the HF chat endpoint.
            // "using var" disposes the request object when this method finishes.
            using var request = new HttpRequestMessage(HttpMethod.Post, ChatUrl);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            // Authorization header: "Bearer" + token.
            // Without this header, Hugging Face will reject the request.
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            // Send the request and read the raw JSON response body.
            var response = await _httpClient.SendAsync(request);
            var responseJson = await response.Content.ReadAsStringAsync();

            // Non-success status codes include 401, 404, 429, 500, 503, etc.
            // We log the problem for debugging and return fallback to the frontend.
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "HF chat LLM returned {StatusCode}: {Body}",
                    (int)response.StatusCode,
                    responseJson);
                return fallback;
            }

            // Parse the JSON response so we can navigate through its properties.
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            // Expected chat response shape:
            // {
            //   "choices": [
            //     { "message": { "role": "assistant", "content": "summary text" } }
            //   ]
            // }
            if (root.TryGetProperty("choices", out var choices)
                && choices.ValueKind == JsonValueKind.Array
                && choices.GetArrayLength() > 0)
            {
                var message = choices[0].GetProperty("message");
                var text = message.GetProperty("content").GetString();

                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text.Trim();
                }
            }

            // If the request succeeded but the response did not contain usable text,
            // keep the app stable and return the deterministic summary.
            _logger.LogWarning("HF chat LLM response had no usable content: {Body}", responseJson);
            return fallback;
        }
        catch (Exception ex)
        {
            // Catch network errors, timeouts, JSON parsing errors, and unexpected HF issues.
            // The user should not see a broken results page just because the AI layer failed.
            _logger.LogWarning(ex, "AI summary failed, using fallback.");
            return fallback;
        }
    }

    // Turns a classifier confidence score into plain-English wording.
    // This is deliberately done in C#, not by the LLM, so the same score always
    // produces the same phrase. That makes the output reproducible and easier
    // to defend: the model explains the result, but our code controls how much
    // certainty is communicated to the user.
    private static string DescribeConfidence(double confidence)
    {
        // Confidence may arrive as 0.56 (fraction) or 56 (percentage).
        // This ternary operator is a short if/else:
        //   condition ? value-if-true : value-if-false
        // After this line, percent is always on a 0-100 scale.
        var percent = confidence <= 1.0 ? confidence * 100.0 : confidence;

        // A switch expression chooses the first matching threshold from top to bottom.
        // "_" means "anything that did not match the previous cases".
        return percent switch
        {
            >= 90 => "high confidence",
            >= 75 => "moderate confidence",
            >= 60 => "limited confidence",
            _ => "low confidence"
        };
    }

    private static (string systemMessage, string userMessage) BuildMessages(
        AnalyseResponse results,
        List<string> userConditions)
    {
        // Known ingredients are already analysed by our deterministic backend.
        // We include only the fields the model is allowed to explain.
        var known = results.Results
    .Select(r =>
    {
        var functionText = string.IsNullOrWhiteSpace(r.Function)
            ? "function not available"
            : r.Function;

        return $"{r.InciName} ({r.SafetyRating}, {r.Category}, function: {functionText})";
    })
    .ToList();

        // Unknown ingredients are not in the database, so their label comes from
        // the existing zero-shot classifier. The confidence WORD is chosen here
        // in C# and then handed to the LLM already decided.
        var unknown = results.UnknownIngredients
            .Select(u =>
            {
                // Normalise the score only for displaying a clean percentage.
                // The DescribeConfidence method does its own normalisation too,
                // so it is safe whether Confidence is stored as 0.56 or 56.
                var percent = u.Confidence <= 1.0 ? u.Confidence * 100.0 : u.Confidence;
                var rounded = (int)Math.Round(percent);
                var confidenceWord = DescribeConfidence(u.Confidence);

                // The escaped \" characters print real double quotes around the AI label.
                return $"{u.Name} - not found in database; AI classifier suggests \"{u.AiLabel}\" with {confidenceWord} ({rounded}%)";
            })
            .ToList();

        // These concerns come from our own condition rules, not from the LLM.
        // This supports the "LLM explains, deterministic system decides" architecture.
        var concerns = results.Results
            .Where(r => r.ConditionFlags.Any(f => f.FlagType == "Avoid"))
            .Select(r => r.InciName)
            .ToList();

        var benefits = results.Results
            .SelectMany(r => r.ConditionFlags
                .Where(f => f.FlagType == "Beneficial")
                .Select(f => $"{r.InciName} ({f.Condition}: {f.Notes})"))
            .ToList();

        var productSignals = BuildProductSignals(results);

        // For now IngredientAnalysisService passes an empty list.
        // Later, if the user opts into personalised summaries, this line will include
        // selected skin concerns such as Rosacea or AtopicDermatitis.
        var conditionsLine = userConditions.Any()
            ? $"User's skin concerns: {string.Join(", ", userConditions)}."
            : "No specific skin profile was provided.";

        // The system message is the safety instruction.
        // It tells the model to treat database-backed facts and AI-classified
        // unknown ingredients differently. Most importantly, the model must use
        // the confidence wording we already chose in C# without making it stronger.
        var systemMessage =
    "You are a cosmetic ingredient assistant. Using ONLY the analysis data the user gives you, " +
    "write a clear structured product overview for a general audience. " +
    "Use exactly these four short sections with labels: 'Formula profile:', 'Main cosmetic roles:', 'Personalised notes:', and 'Data limitations:'. " +
    "Write 1-2 concise sentences per section. " +

    "This is cosmetic ingredient information, not medical advice. " +
    "Do not claim that the product treats, heals, soothes, calms, alleviates, prevents, cures, improves, or manages any medical condition or symptoms. " +
    "Do not say that the product is suitable for atopic dermatitis, eczema, psoriasis, rosacea, acne, alopecia, seborrhoeic dermatitis, or any other condition. " +
    "Do not recommend the product for a medical or skin condition. " +

    "You may describe ingredient functions only as cosmetic functions, for example hydration, humectant effect, smoothing, cleansing, preservative, fragrance, or exfoliation. " +
    "Use cautious wording such as 'contains ingredients commonly used for', 'is flagged as a potential concern', or 'may be relevant for cosmetic hydration'. " +

    "If any Avoid flags are present, clearly mention that the product has mixed relevance for the selected profile. " +
    "If Beneficial flags are present, you may mention them as cosmetic profile-supporting signals, but do not claim the product treats, improves, manages, or is suitable for a medical condition. " +
    "When mentioning Beneficial flags, use cautious wording such as 'contains ingredients flagged as beneficial in the cosmetic rule set' or 'may be relevant to the selected profile's cosmetic needs'. " +
    "If a fragrance or parfum ingredient is flagged, mention it as a potential concern for the selected profile. " +
    "Only ingredients explicitly listed under 'Potential concerns (Avoid flags)' may be described as concerns for the selected profile. " +
    "Never describe an individual ingredient as having 'mixed relevance' unless that ingredient is explicitly listed under 'Potential concerns (Avoid flags)'. " +
    "The phrase 'mixed relevance for the selected profile' may describe the product as a whole only when at least one Avoid flag is present, and the reason must be an ingredient from that Avoid list. " +
    "If 'Potential concerns (Avoid flags)' is 'none', the Personalised notes section must say that no ingredients were specifically flagged as Avoid for the selected profile, and must not use the phrase 'mixed relevance'. " +
    "A SafetyRating or a regulatory category such as Amber, Restricted Substance, Preservative, or Keratolytic is not by itself evidence of irritation or incompatibility with sensitive skin. " +
    "Restricted Substance is a regulatory classification only. " +
    "If you mention a restricted ingredient, say only that regulatory conditions or concentration limits may apply; do not turn that classification into a medical or personalised warning. " +
    "A restricted ingredient may be mentioned under Data limitations or regulatory notes, but never as the reason for a personalised profile concern unless the same ingredient is explicitly listed under Potential concerns (Avoid flags). " +
    "Do not translate 'skin conditioning' or another listed function into hydration unless HUMECTANT or another explicit hydration-related function is present. " +
    "CosIng functions are possible listed functions for an ingredient in cosmetics, not proof of the ingredient's exact purpose in this specific product. " +
    "If a function list contains context-specific uses such as hair conditioning or oral care, do not mention those unless the Product signals line supports that product context. " +

    "Do not infer the product type, such as sunscreen, cleanser, or fragrance-based formula, unless the Product signals line explicitly supports it. " +
    "If the Product signals line says the formula is likely a sunscreen or SPF product because it contains multiple UV filters, you may explain that as a likely cosmetic purpose. " +
    "Known ingredients come from a curated database and can be described with more confidence. " +
    "Ingredients marked 'not found in database' come from a general AI classifier; they are estimates, not verified facts. " +
    "For those ingredients, a confidence wording is already provided, for example 'low confidence'. " +
    "Use that wording exactly as given and do not make the ingredient sound more or less certain than that wording. " +
    "If any unknown ingredients are provided, always mention them briefly, including that they were not found in the database and that the AI classifier result is unverified. " +

    "Never claim the product is safe, effective, or suitable for a condition. " +
    "Do not invent any ingredient, effect, or fact that is not in the data.";

        // The user message contains the facts for this product.
        // Notice the wording: we are not asking the model to analyse from scratch.
        // We are giving it our analysis and asking it to turn that into readable text.
        var userMessage =
            $"Product signals: {(productSignals.Any() ? string.Join("; ", productSignals) : "none")}.\n" +
            $"Known ingredients (from database): {(known.Any() ? string.Join(", ", known) : "none")}.\n" +
            $"Unknown ingredients (AI-classified): {(unknown.Any() ? string.Join("; ", unknown) : "none")}.\n" +
            $"Potential concerns (Avoid flags): {(concerns.Any() ? string.Join(", ", concerns) : "none")}.\n" +
            $"Beneficial profile signals: {(benefits.Any() ? string.Join("; ", benefits) : "none")}.\n" +
            $"Personalised interpretation rule: {(concerns.Any() ? "Avoid flags are present, so personalised concerns may be mentioned only for those listed ingredients." : "There are no Avoid flags. Do not say mixed relevance for the selected profile. Do not treat regulatory restrictions as personalised concerns.")}\n" +
            $"{conditionsLine}";

        return (systemMessage, userMessage);
    }

    private static List<string> BuildProductSignals(AnalyseResponse results)
    {
        var signals = new List<string>();

        var uvFilters = results.Results
            .Where(r => HasCategory(r, "UV Filter"))
            .Select(r => r.InciName)
            .ToList();

        var humectants = results.Results
            .Where(r => HasCategory(r, "Humectants"))
            .Select(r => r.InciName)
            .ToList();

        var emollients = results.Results
            .Where(r => HasCategory(r, "Emollients"))
            .Select(r => r.InciName)
            .ToList();

        if (uvFilters.Count >= 3)
        {
            signals.Add($"Likely sunscreen/SPF product: {uvFilters.Count} UV filters identified ({string.Join(", ", uvFilters.Take(5))})");
        }

        if (humectants.Count > 0)
        {
            signals.Add($"Hydration-related formula: humectants identified ({string.Join(", ", humectants.Take(5))})");
        }

        if (emollients.Count > 0)
        {
            signals.Add($"Skin-conditioning/emollient base: emollients identified ({string.Join(", ", emollients.Take(5))})");
        }

        return signals;
    }

    private static bool HasCategory(IngredientResultDto result, string category)
    {
        return result.Category
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(c => string.Equals(c, category, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildFallbackSummary(AnalyseResponse results)
    {
        // This fallback is deterministic: it uses only our backend's own results.
        // No AI model is involved here, so it is always available.
        var known = results.Results.Count;
        var unknown = results.UnknownIngredients.Count;
        var total = known + unknown;

        var concerns = results.Results
            .Where(r => r.ConditionFlags.Any(f => f.FlagType == "Avoid"))
            .Select(r => r.InciName)
            .ToList();

        var productSignals = BuildProductSignals(results);

        var summary =
            $"Formula profile: {(productSignals.Any() ? string.Join("; ", productSignals) : "No strong product-type signal was detected from the available categories.")}\n" +
            $"Main cosmetic roles: This product contains {total} ingredient(s): {known} found in the database and {unknown} not recognised.\n";

        summary += concerns.Any()
            ? $"Personalised notes: Potential concerns based on the selected profile: {string.Join(", ", concerns)}.\n"
            : "Personalised notes: No specific Avoid flags were identified in the known ingredients for the selected profile.\n";

        summary += "Data limitations: This is a preliminary cosmetic ingredient assessment based on available database matches and does not replace professional advice.";

        return summary;
    }
}
