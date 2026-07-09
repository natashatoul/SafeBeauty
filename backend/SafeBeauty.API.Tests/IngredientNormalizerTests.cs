using SafeBeauty.API.Models;

namespace SafeBeauty.API.Tests;

public class IngredientNormalizerTests
{
    [Theory]
    [InlineData("retinol", "RETINOL")]
    [InlineData("Retinol", "RETINOL")]
    [InlineData(" RETINOL ", "RETINOL")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData(null, "")]
    public void Normalize_ReturnsCanonicalInciName(
        string? input,
        string expected)
    {
        var result = IngredientNormalizer.Normalize(input!);

        Assert.Equal(expected, result);
    }
}
