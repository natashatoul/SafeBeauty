using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SafeBeauty.API.DTOs;
using SafeBeauty.API.Models;
using SafeBeauty.API.Services;

namespace SafeBeauty.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductsController : ControllerBase
{
    private readonly IngredientAnalysisService _analysisService;
    private readonly HttpClient _httpClient;
    public ProductsController(IngredientAnalysisService analysisService, IHttpClientFactory httpClientFactory)
    {
        _analysisService = analysisService;
        _httpClient = httpClientFactory.CreateClient();
    }

        //POST - api/Products/barcode/{barcode}
    // Fetches product from Open Beauty Facts API and analises ingredients.
    // POST (not GET) so conditions/ageGroup/gender travel in the body,
    // not in the URL — query strings end up in server access logs.
    [HttpPost("barcode/{barcode}")]
    public async Task<ActionResult<ProductAnalyseResponse>> GetByBarcode(
        string barcode,
        [FromBody] BarcodeAnalyseRequest? request)
    {
        var body = request ?? new BarcodeAnalyseRequest();

        if (!BarcodeValidator.TryValidate(barcode, out var validationError))
        {
            return BadRequest(validationError);
        }

        var url = $"https://world.openbeautyfacts.org/api/v0/product/{Uri.EscapeDataString(barcode)}.json";
        HttpResponseMessage response;

        try
        {
            response = await _httpClient.GetAsync(url);
        }
        catch (TaskCanceledException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                "Open Beauty Facts timed out. Please try again later.");
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                "Open Beauty Facts is temporarily unavailable.");
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound("Product not found in Open Beauty Facts database.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode(StatusCodes.Status502BadGateway,
                    $"Open Beauty Facts returned status {(int)response.StatusCode}.");
            }

            var json = await response.Content.ReadAsStringAsync();

            try
            {
                return await BuildProductResponse(barcode, json, body.UserConditions, body.AgeGroup, body.Gender);
            }
            catch (JsonException)
            {
                return StatusCode(StatusCodes.Status502BadGateway,
                    "Open Beauty Facts returned an invalid response.");
            }
        }
    }


    private async Task<ActionResult<ProductAnalyseResponse>> BuildProductResponse(
        string barcode,
        string json,
        List<string>? userConditions,
        string? ageGroup,
        string? gender)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("status", out var statusEl) ||
            !statusEl.TryGetInt32(out var status))
        {
            return StatusCode(StatusCodes.Status502BadGateway, "Unexpected response from Open Beauty Facts.");
        }

        // Check if product was found (status 0 = not found, 1 - found)
        if (status == 0)
        {
            return NotFound("Product not found in Open Beauty Facts database.");
        }

        if (status != 1)
        {
            return StatusCode(StatusCodes.Status502BadGateway, "Unexpected response from Open Beauty Facts.");
        }

        if (!root.TryGetProperty("product", out var product))
        {
            return StatusCode(StatusCodes.Status502BadGateway, "Product data is missing from Open Beauty Facts response.");
        }

        // Extract product name
        var productName = product.TryGetProperty("product_name", out var nameEl)
        ? nameEl.GetString() ?? string.Empty : string.Empty;

        // Extract ingredient names from the structured ingredients array.
        // Open Beauty Facts / Open Beauty Facts often contains a parsed
        // ingredients array, but not always.
        var ingredientNames = new List<string>();
        if (product.TryGetProperty("ingredients", out var ingredientsEl) &&
            ingredientsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in ingredientsEl.EnumerateArray())
            {
                if (item.TryGetProperty("text", out var textEl))
                {
                    var text = textEl.GetString();
                    if (!string.IsNullOrWhiteSpace(text)) ingredientNames.Add(text);
                }
            }
        }

        // Fallback: many Open Beauty Facts records store ingredients only as
        // a raw text field, for example "Aqua, Glycerin, Niacinamide".
        // Without this fallback, valid products can be found by barcode but
        // still produce an empty analysis.
        if (ingredientNames.Count == 0)
        {
            ingredientNames.AddRange(ExtractIngredientsFromText(product));
        }

        // Analyse extracted ingredients
        var analysis = await _analysisService.AnalyseAsync(
            ingredientNames,
            userConditions,
            ageGroup,
            gender);
        return new ProductAnalyseResponse
        {
            ProductName = productName,
            Barcode = barcode,
            SourceIngredients = ingredientNames,
            Analysis = analysis
        };
    }

    private static IEnumerable<string> ExtractIngredientsFromText(JsonElement product)
    {
        var textFields = new[]
        {
            "ingredients_text",
            "ingredients_text_en",
            "ingredients_text_fr"
        };

        foreach (var field in textFields)
        {
            if (!product.TryGetProperty(field, out var textEl)) continue;

            var raw = textEl.GetString();
            if (string.IsNullOrWhiteSpace(raw)) continue;

            return IngredientListParser.Parse(new[] { raw });
        }

        return Enumerable.Empty<string>();
    }

}
