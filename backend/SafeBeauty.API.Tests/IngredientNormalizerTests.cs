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
    [InlineData("Alcohol Denat.", "ALCOHOL DENAT")]
    [InlineData("Coco-Caprylate Caprate", "COCO-CAPRYLATE/CAPRATE")]
    [InlineData("Coco-Caprylate/Caprate", "COCO-CAPRYLATE/CAPRATE")]
    [InlineData("Lanolin Alcohol (Eucerit®)", "LANOLIN ALCOHOL")]
    [InlineData("Lanolin Alcohol (Example™)", "LANOLIN ALCOHOL")]
    [InlineData("AQUA / WATER / EAU", "AQUA")]
    [InlineData("AQUA/WATER/EAU", "AQUA")]
    [InlineData("COPERNICIA CERIFERA CERA / CARNAUBA WAX / CIRE DE CARNAUBA", "COPERNICIA CERIFERA CERA")]
    [InlineData("BUTYROSPERMUM PARKII BUTTER / SHEA BUTTER", "BUTYROSPERMUM PARKII BUTTER")]
    [InlineData("XANTHAN GUM (F.I.L. N70032039/1).", "XANTHAN GUM")]
    [InlineData("VITIS VINIFERA (GRAPE) SEED OIL", "VITIS VINIFERA SEED OIL")]
    [InlineData("HELIANTHUS ANNUUS (SUNFLOWER) SEED OIL", "HELIANTHUS ANNUUS SEED OIL")]
    [InlineData("TOCOPHEROL.(409/011)", "TOCOPHEROL")]
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
