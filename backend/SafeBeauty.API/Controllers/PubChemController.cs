using Microsoft.AspNetCore.Mvc;
using SafeBeauty.API.Services;

namespace SafeBeauty.API.Controllers;

[ApiController]
[Route("api/pubchem")]
public class PubChemController : ControllerBase
{
    private readonly PubChemService _pubChemService;

    public PubChemController(PubChemService pubChemService)
    {
        _pubChemService = pubChemService;
    }

    [HttpGet("description")]
    public async Task<IActionResult> GetDescription(
        [FromQuery] string ingredient,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ingredient))
            return BadRequest("Ingredient name is required.");

        var descriptions =
            await _pubChemService.GetDescriptionsAsync(
                ingredient, cancellationToken);

        return Ok(new
        {
            ingredient,
            descriptions
        });
    }

    
[HttpPost("batch")]
public async Task<IActionResult> GetBatchDescriptions(
    [FromBody] List<string> ingredients,
    CancellationToken cancellationToken)
{
    if (ingredients == null || ingredients.Count == 0)
        return BadRequest("Ingredient list is required.");

    if (ingredients.Count > 100)
        return BadRequest("Maximum 100 ingredients.");

    var results = new List<object>();

    foreach (var ingredient in ingredients
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Select(x => x.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase))
    {
        try
        {
            var descriptions =
                await _pubChemService.GetDescriptionsAsync(
                    ingredient, cancellationToken);

            results.Add(new
            {
                ingredient,
                status = descriptions.Count > 0
                    ? "DescriptionFound"
                    : "NoDescription",
                descriptions
            });
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            results.Add(new
            {
                ingredient,
                status = "Error",
                descriptions = new List<PubChemDescription>()
            });
        }

        // Approximate request pacing for this sequential test.
        await Task.Delay(250, cancellationToken);
    }

    return Ok(results);
}

}