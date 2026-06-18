using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SafeBeauty.API.DTOs;
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

    //GET - api/Products/barcode/{barcode}
    // Fetches product from Open Beauty Facts API and analises ingredients
    [HttpGet("barcode/{barcode}")]
    public async Task<ActionResult<ProductAnalyseResponse>> GetByBarcode(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        return BadRequest("Barcode cannot be empty.");

        var url = $"https://world.openbeautyfacts.org/api/v0/product/{barcode}.json";

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        return NotFound($"API error. Status: {response.StatusCode}");

        var json = await response.Content.ReadAsStringAsync();

        // Parse JSON using System.Text.Json (C# library)
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("status", out var statusEl) ||
            !statusEl.TryGetInt32(out var status))
        return StatusCode(StatusCodes.Status502BadGateway, "Unexpected response from Open Beauty Facts.");

        // Check if product was found (status 0 = not found, 1 - found)
        if (status == 0)
        return NotFound("Product not found in Open Beauty Facts database.");

        if (!root.TryGetProperty("product", out var product))
        return StatusCode(StatusCodes.Status502BadGateway, "Product data is missing from Open Beauty Facts response.");

        // Extract product name
        var productName = product.TryGetProperty("product_name", out var nameEl)
        ? nameEl.GetString() ?? string.Empty : string.Empty;

        // Extrack ingredient names from ingredients array
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

        // Analyse extracted ingredients
        var analysis = await _analysisService.AnalyseAsync(ingredientNames);
        return Ok(new ProductAnalyseResponse
        {
            ProductName = productName,
            Barcode = barcode,
            Analysis = analysis
        });

    }

}
