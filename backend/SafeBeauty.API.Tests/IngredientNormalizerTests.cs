using SafeBeauty.API.Models;

namespace SafeBeauty.API.Tests;

public class IngredientNormalizerTests
{
    [Theory]
    [InlineData("retinol", "RETINOL")]
    [InlineData("Retinol", "RETINOL")]
    [InlineData(" RETINOL ", "RETINOL")]
    [InlineData("Aqua (Water)", "AQUA")]
    [InlineData("Parfum (Fragrance)", "PARFUM")]
    [InlineData("Butyrospermum Parkii (Shea) Butter", "BUTYROSPERMUM PARKII BUTTER")]
    [InlineData("Palmitoyl Tetrapeptide-95.", "PALMITOYL TETRAPEPTIDE-95")]
    [InlineData("Methylene bis-benzotriazolyl tetramethylbutylphenol (nano)", "METHYLENE BIS-BENZOTRIAZOLYL TETRAMETHYLBUTYLPHENOL (NANO)")]
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
