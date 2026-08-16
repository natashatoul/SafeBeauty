using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SafeBeauty.API.DTOs;

namespace SafeBeauty.API.Services;

public class AiSummaryService
{
    // HttpClient is the .NET object that sends HTTP requests.
    private readonly HttpClient _httpClient;

    // Hugging Face LLM token from configuration.
    private readonly string _apiKey;

    // Logger lets us record what went wrong without crashing the application.
    // The user still sees the fallback summary, while we can inspect the server logs.
    private readonly ILogger<AiSummaryService> _logger;

    // Hugging Face's OpenAI-compatible chat endpoint.
    // This endpoint expects "messages" with roles like system/user/assistant.
    private readonly string _chatUrl = "https://router.huggingface.co/v1/chat/completions";

    // The model that writes the product summary.
    // If this model is unavailable for your token/provider later, this is the
    // one string we can swap without changing the rest of the service.
    private readonly string _modelId = "meta-llama/Llama-3.1-8B-Instruct";

    // Only values offered by ProfilePage may enter the model prompt. Besides
    // keeping the wording consistent, this prevents arbitrary user text from
    // being treated as prompt instructions.
    private static readonly HashSet<string> AllowedAgeGroups = new(StringComparer.Ordinal)
    {
        "Under 18", "18-25", "26-35", "36-45", "46-60", "60+"
    };
    private static readonly HashSet<string> AllowedGenders = new(StringComparer.Ordinal)
    {
        "Female", "Male", "Prefer not to say"
    };
    private static readonly string[] ProhibitedPhrases =
    {
        "suitable for",
        "suitable option",
        "recommended for",
        "designed to",
        "beneficial for",
        "broad-spectrum protection",
        "broad spectrum protection",
        "protection against uva and uvb",
        "protects against uva and uvb",
        "provides uva protection",
        "provides uvb protection",
        "eczema care",
        "for eczema",
        "for atopic dermatitis",
        "treats ",
        "treat ",
        "heals ",
        "cures ",
        "manages ",
        "improves symptoms"
    };
    private static readonly string[] BeneficialPhrases =
    {
        "flagged as beneficial",
        "beneficial profile signal",
        "profile-supporting signal",
        "relevant to the selected profile"
    };


// this is constructor
    public AiSummaryService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AiSummaryService> logger)
    {
        _httpClient = httpClient;
        _apiKey = configuration["HuggingFace:LlmApiKey"] ?? string.Empty;
        _logger = logger;
        _chatUrl = configuration["HuggingFace:ChatUrl"] ?? "https://router.huggingface.co/v1/chat/completions";
        _modelId = configuration["HuggingFace:ModelId"] ?? "meta-llama/Llama-3.1-8B-Instruct";

    }

// this method prepare request form(how to make JSON from text for sertain roles) 
// + model/max_tokens/temperature and send this form via HTTP
    public async Task<string> SummariseAsync(
        AnalyseResponse results,
        IReadOnlyCollection<string> userConditions,
        string? ageGroup = null,
        string? gender = null)
    {
        // Always build a deterministic fallback first.
        // This is a safety net: if Hugging Face is slow, unavailable, unauthorised,
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
            //   systemMessage, like rules for the model ("do not invent facts")
            //   userMessage, the actual analysis data for this product
            var (systemMessage, userMessage) = BuildMessages(
                results,
                userConditions,
                ageGroup,
                gender);

            // Anonymous object: a temporary C# object that matches the JSON shape
            // expected by the Hugging Face chat endpoint.
            var requestBody = new
            {
                model = _modelId,
                messages = new[]
                {
                    // "system" is the instruction layer: it tells the model how to behave.
                    new { role = "system", content = systemMessage },

                    // "user" is the data layer: it contains this product's analysis.
                    new { role = "user", content = userMessage }
                },

                max_tokens = 280,

                // Low temperature makes the model less creative and more predictable.
                // For safety-related explanations, we want cautious wording, not imagination.
                temperature = 0.3
            };

            // Convert the C# object into JSON text before sending it over HTTP.
            var json = JsonSerializer.Serialize(requestBody);

            // Create one HTTP POST request to the HF chat endpoint.
            // "using var" disposes the request object when this method finishes.
            using var request = new HttpRequestMessage(HttpMethod.Post, _chatUrl);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            // Authorization header: "Bearer" + token.
            // Without this header, Hugging Face will reject the request.
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            // Send the request and read the raw JSON response body.
            var response = await _httpClient.SendAsync(request);
            var responseJson = await response.Content.ReadAsStringAsync();

            // Non-success status codes include 401, 404, 429, 500, 503, etc.
            // Log the problem for debugging and return fallback to the frontend.
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "HF chat LLM returned status code {StatusCode}.",
                    (int)response.StatusCode);

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
                    var summary = CleanSummaryOpening(text.Trim());
                    if (ViolatesSafetyBoundary(summary) || ContradictsProfileFlags(summary, results))
                    {
                        // Do not include the rejected model text in logs. It may contain
                        // optional profile details such as age group or gender.
                        _logger.LogWarning(
                            "AI summary crossed a medical, suitability, or unsupported efficacy boundary; using fallback.");
                        return fallback;
                    }

                    return summary;
                }
            }

            // If the request succeeded but the response did not contain usable text,
            // keep the app stable and return the deterministic summary.
            _logger.LogWarning("HF chat LLM response had no usable content.");

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


// this method takes facts(results, userConditions, ageGroup, gender) and create two 
// bis strings - systemMessage(rules) and userMessage(ingredients. flags. profile)
// and retuurns together
    private static (string systemMessage, string userMessage) BuildMessages(
        AnalyseResponse results,
        IReadOnlyCollection<string> userConditions,
        string? ageGroup,
        string? gender)
    {
        var known = results.Results
            .Select(result =>
                $"{result.InciName}: {string.Join(", ", GetPlainRoles(result))}; " +
                $"regulatory rating {result.SafetyRating}")
            .ToList();

        var concerns = results.Results
            .Where(r => r.ConditionFlags.Any(f => f.FlagType == "Avoid"))
            .Select(r => r.InciName)
            .ToList();

        var benefits = results.Results
            .SelectMany(r => r.ConditionFlags
                .Where(f => f.FlagType == "Beneficial")
                .Select(_ => $"{r.InciName} (category-level profile rule)"))
            .ToList();

        var productSignals = BuildProductSignals(results);

        var conditionsLine = userConditions.Any()
            ? $"A profile with {userConditions.Count} selected condition(s) was provided. Refer to it only as 'the selected profile'; do not repeat condition names."
            : "No specific skin profile was provided.";
        var presentationContextLine = BuildPresentationContextLine(ageGroup, gender);

// it is a prompt, instuction that send to the model before generation the response
        var systemMessage =
    "You are a cosmetic ingredient assistant. Using ONLY the verified analysis facts supplied by the user, " +
    "write one cohesive consumer-friendly paragraph of about 140-190 words that explains the overall formula, " +
    "its main cosmetic roles, and what matters for the selected profile. Do not use headings or bullet points. " +
    "Start with a complete standalone sentence. Do not begin with transition phrases such as 'It also', 'Additionally', 'Moreover', or 'Furthermore'. " +

    "This is cosmetic ingredient information, not medical advice. " +
    "Do not claim that the product treats, heals, soothes, calms, alleviates, prevents, cures, improves, manages, or is recommended for any medical condition or symptoms. " +
    "Do not say that the product is suitable for atopic dermatitis, eczema, psoriasis, rosacea, acne, alopecia, seborrhoeic dermatitis, or any other condition. " +

    "Age group and gender are optional presentation context that may influence which verified cosmetic information is emphasised in the explanation. They must never introduce new ingredient facts, regulatory ratings, condition flags, medical claims, or unsupported suitability or efficacy conclusions. " +

    "Never combine age or gender with words such as suitable, recommended, effective, beneficial, designed for, or intended for. " +
    "Gender may affect neutral wording only; prefer second-person wording such as 'your skin'. Do not describe 'male skin' or 'female skin', and do not claim that a gender inherently has particular needs or properties. " +
    "If age is mentioned, copy the supplied age-group label exactly. Do not translate a range such as '46-60' into 'women in their 40s and 50s'. " +
    "Age group may be mentioned only to frame cosmetic roles already present in the verified data. Do not infer mature skin, young skin, wrinkles, anti-ageing performance, or age-specific suitability from age alone. " +

    "You may describe ingredient functions only as cosmetic functions, for example hydration, humectant effect, smoothing, cleansing, preservative, fragrance, or exfoliation. " +
    "Use cautious wording such as 'contains ingredients commonly used for', 'is flagged as a potential concern', or 'may be relevant for cosmetic hydration'. " +
    "Do not say that the formula or product is 'designed to' produce an effect; an ingredient list does not establish product intent, concentration, or performance. " +

    "If any Avoid flags are present, clearly mention that the product has mixed relevance for the selected profile. " +
    "If Beneficial flags are present, you may mention them as cosmetic profile-supporting signals, but do not claim the product treats, improves, manages, or is suitable for a medical condition. " +
    "When mentioning Beneficial flags, use cautious wording such as 'contains ingredients flagged as beneficial in the cosmetic rule set' or 'may be relevant to the selected profile's cosmetic needs'. " +
    "Only ingredients explicitly listed under 'Beneficial profile signals' may be described as beneficial, profile-supporting, or relevant to the selected profile. " +
    "Do not transfer a Beneficial flag from one ingredient, category, or list to another ingredient. " +
    "If 'Beneficial profile signals' is 'none', do not make any beneficial or profile-supporting claim. " +
    "A Beneficial flag may come from a broad category-level rule. It is not proof that an individual ingredient or the finished product benefits, treats, or is suitable for the condition. " +
    "If a fragrance or parfum ingredient is flagged, mention it as a potential concern for the selected profile. " +
    "Only ingredients explicitly listed under 'Potential concerns (Avoid flags)' may be described as concerns for the selected profile. " +
    "Never describe an individual ingredient as having 'mixed relevance' unless that ingredient is explicitly listed under 'Potential concerns (Avoid flags)'. " +
    "The phrase 'mixed relevance for the selected profile' may describe the product as a whole only when at least one Avoid flag is present, and the reason must be an ingredient from that Avoid list. " +
    "If 'Potential concerns (Avoid flags)' is 'none', say that no ingredients were specifically flagged as Avoid for the selected profile, and do not use the phrase 'mixed relevance'. " +
    
    "A SafetyRating or a regulatory category such as Amber, Restricted Substance, Preservative, or Keratolytic is not by itself evidence of irritation or incompatibility with sensitive skin. " +
    "Restricted Substance is a regulatory classification only: mention only that regulatory conditions or concentration limits may apply, and never treat it as a personalised profile concern unless the same ingredient is explicitly listed under Potential concerns (Avoid flags). " +

    "Do not translate 'skin conditioning' or another listed function into hydration unless HUMECTANT or another explicit hydration-related function is present. " +
    "CosIng functions are possible listed functions for an ingredient in cosmetics, not proof of the ingredient's exact purpose in this specific product. " +
    "If a function list contains context-specific uses such as hair conditioning or oral care, do not mention those unless the Product signals line supports that product context. " +

    "Do not infer the product type, such as sunscreen, cleanser, or fragrance-based formula, unless the Product signals line explicitly supports it. The presence of UV filters alone does not prove that the finished product is a sunscreen. " +
    "If confirmed Annex VI UV filters are supplied, explain their mineral, organic, or organic particulate type without claiming an SPF value, broad-spectrum protection, UVA/UVB performance, or any exact protection level. " +
    "Known ingredient roles are deterministic summaries of curated categories and possible CosIng functions. " +
    "Unmatched ingredient names are intentionally not supplied because they may contain OCR errors. " +
    "If the unmatched count is above zero, mention only the count and that those names could not be verified. " +
    "Do not invent, reconstruct, quote, or classify any unmatched ingredient name. " +

    "Never claim the product is safe, effective, or suitable for a condition, and do not invent any ingredient, effect, or fact that is not in the data.";

        var total = results.Results.Count + results.UnknownIngredients.Count;
        var unknownShare = total == 0
            ? 0
            : (int)Math.Round(results.UnknownIngredients.Count * 100d / total);

        var userMessage =
            $"Product signals: {(productSignals.Any() ? string.Join("; ", productSignals) : "none")}.\n" +
            $"Verified ingredients and plain roles: {(known.Any() ? string.Join("; ", known) : "none")}.\n" +
            $"Data coverage: {results.Results.Count} of {total} ingredients matched; {results.UnknownIngredients.Count} unmatched ({unknownShare}%). Raw unmatched names are not provided.\n" +
            $"Potential concerns (Avoid flags): {(concerns.Any() ? string.Join(", ", concerns) : "none")}.\n" +
            $"Beneficial profile signals: {(benefits.Any() ? string.Join("; ", benefits) : "none")}.\n" +
            $"Personalised interpretation rule: {(concerns.Any() ? "Avoid flags are present, so personalised concerns may be mentioned only for those listed ingredients." : "There are no Avoid flags. Do not say mixed relevance for the selected profile. Do not treat regulatory restrictions as personalised concerns.")}\n" +
            $"{conditionsLine}\n" +
            $"{presentationContextLine}";

        return (systemMessage, userMessage);
    }

    private static string BuildPresentationContextLine(string? ageGroup, string? gender)
    {
        var safeAgeGroup = AllowedAgeGroups.Contains(ageGroup ?? string.Empty)
            ? ageGroup
            : null;
        var safeGender = AllowedGenders.Contains(gender ?? string.Empty)
            && gender != "Prefer not to say"
            ? gender
            : null;

        if (safeAgeGroup is null && safeGender is null)
        {
            return "No optional age or gender presentation context was provided.";
        }

        var values = new List<string>();
        if (safeAgeGroup is not null) values.Add($"age group {safeAgeGroup}");
        if (safeGender is not null) values.Add($"gender {safeGender}");

        return $"Optional presentation context (wording only): {string.Join("; ", values)}. " +
            "Do not use it to create or alter ingredient claims.";
    }

    private static List<string> BuildProductSignals(AnalyseResponse results)
    {
        var signals = new List<string>();

        var uvFilters = results.Results
            .Where(r => r.IsUvFilter)
            .Select(r => $"{r.InciName} ({r.UvFilterType})")
            .ToList();

        var humectants = results.Results
            .Where(r => HasCategory(r, "Humectants"))
            .Select(r => r.InciName)
            .ToList();

        var emollients = results.Results
            .Where(r => HasCategory(r, "Emollients"))
            .Select(r => r.InciName)
            .ToList();

        if (uvFilters.Count > 0)
        {
            signals.Add($"Confirmed Annex VI UV filters: {uvFilters.Count} identified ({string.Join(", ", uvFilters.Take(5))})");
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

    private static List<string> GetPlainRoles(IngredientResultDto result)
    {
        var roles = new List<string>();

        if (HasCategory(result, "Humectants") || HasFunction(result, "HUMECTANT"))
            roles.Add("moisture support");
        if (HasCategory(result, "Emollients") ||
            HasFunction(result, "SKIN CONDITIONING - EMOLLIENT") ||
            HasFunction(result, "SKIN PROTECTING"))
            roles.Add("softening or moisture-loss reduction");
        if (HasFunction(result, "EMULSION STABILISING") ||
            HasFunction(result, "VISCOSITY CONTROLLING") ||
            HasFunction(result, "SURFACTANT - EMULSIFYING") ||
            HasFunction(result, "GEL FORMING"))
            roles.Add("texture or formula stability");
        if (HasCategory(result, "Preservative"))
            roles.Add("product preservation");
        if (HasCategory(result, "Fragrance"))
            roles.Add("fragrance");
        if (result.IsUvFilter)
            roles.Add($"confirmed {result.UvFilterType.ToLowerInvariant()} UV filter");

        if (roles.Count == 0) roles.Add("other supporting cosmetic role");
        return roles;
    }

    private static bool HasFunction(IngredientResultDto result, string function) =>
        result.Function
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(value => string.Equals(value, function, StringComparison.OrdinalIgnoreCase));

    private static bool ViolatesSafetyBoundary(string summary)
    {
        return ProhibitedPhrases.Any(phrase =>
            summary.Contains(phrase, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContradictsProfileFlags(string summary, AnalyseResponse results)
    {
        var hasAvoidFlags = results.Results
            .Any(result => result.ConditionFlags.Any(flag => flag.FlagType == "Avoid"));

        if (hasAvoidFlags)
        {
            if (summary.Contains(
                "no ingredients were specifically flagged as avoid",
                StringComparison.OrdinalIgnoreCase)
                || summary.Contains("no avoid flags", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        else if (summary.Contains("mixed relevance", StringComparison.OrdinalIgnoreCase)
            || summary.Contains("flagged as a potential concern", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return ContradictsBeneficialFlags(summary, results);
    }

    private static string CleanSummaryOpening(string summary)
    {
        var cleaned = summary.Trim();

        cleaned = Regex.Replace(
            cleaned,
            @"^(?:It also includes|Also includes)\b",
            "This formula includes",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        cleaned = Regex.Replace(
            cleaned,
            @"^(?:Additionally|Moreover|Furthermore|Also),?\s+",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        if (cleaned.StartsWith("includes ", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = "This formula " + cleaned;
        }

        cleaned = cleaned.Trim();
        return cleaned.Length == 0
            ? cleaned
            : char.ToUpperInvariant(cleaned[0]) + cleaned[1..];
    }

    private static bool ContradictsBeneficialFlags(string summary, AnalyseResponse results)
    {
        var beneficialNames = results.Results
            .Where(result => result.ConditionFlags.Any(flag => flag.FlagType == "Beneficial"))
            .Select(result => result.InciName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var claimSentences = Regex
            .Split(summary, @"(?<=[.!?])\s+")
            .Where(sentence => BeneficialPhrases.Any(phrase =>
                sentence.Contains(phrase, StringComparison.OrdinalIgnoreCase)));

        foreach (var sentence in claimSentences)
        {
            if (beneficialNames.Count == 0)
            {
                return true;
            }

            var mentionsUnflaggedIngredient = results.Results.Any(result =>
                sentence.Contains(result.InciName, StringComparison.OrdinalIgnoreCase)
                && !beneficialNames.Contains(result.InciName));

            if (mentionsUnflaggedIngredient)
            {
                return true;
            }
        }

        return false;
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

        var uvFilters = results.Results.Where(result => result.IsUvFilter).ToList();
        var emollients = results.Results
            .Where(result => HasCategory(result, "Emollients"))
            .Select(result => result.InciName)
            .Take(5)
            .ToList();
        var humectants = results.Results
            .Where(result => HasCategory(result, "Humectants") || HasFunction(result, "HUMECTANT"))
            .Select(result => result.InciName)
            .Take(5)
            .ToList();

        var paragraphs = new List<string>();

        if (uvFilters.Count > 0)
        {
            var filterTypes = string.Join(", ", uvFilters
                .Select(filter => filter.UvFilterType.ToLowerInvariant())
                .Distinct());
            paragraphs.Add(
                $"The verified composition contains {uvFilters.Count} confirmed {filterTypes} UV filter" +
                $"{(uvFilters.Count == 1 ? string.Empty : "s")}. This identifies filter ingredients, but the list alone cannot confirm the finished product's SPF or protection level.");
        }

        if (emollients.Count > 0)
        {
            var prefix = paragraphs.Count == 0
                ? "This formula includes"
                : "It also includes";
            paragraphs.Add(
                $"{prefix} emollients such as {string.Join(", ", emollients)}, which are commonly used to soften the skin surface and reduce moisture loss.");
        }

        if (humectants.Count > 0)
        {
            paragraphs.Add(
                $"Humectants such as {string.Join(", ", humectants)} are commonly used to help attract or retain water in a cosmetic formula.");
        }

        if (paragraphs.Count == 0)
        {
            paragraphs.Add("No single dominant formula role could be confirmed from the available categories.");
        }

        paragraphs.Add(concerns.Any()
            ? $"For the selected profile, {string.Join(", ", concerns)} " +
              $"{(concerns.Count == 1 ? "was" : "were")} specifically flagged as a potential concern."
            : "No verified ingredients were specifically flagged as Avoid for the selected profile.");

        paragraphs.Add(
            $"The analysis matched {known} of {total} ingredients" +
            (unknown > 0 ? $", while {unknown} could not be verified" : string.Empty) +
            ". Cosmetic functions describe possible roles; they do not reveal concentrations or prove suitability for a medical condition.");

        return string.Join(" ", paragraphs);
    }
}
