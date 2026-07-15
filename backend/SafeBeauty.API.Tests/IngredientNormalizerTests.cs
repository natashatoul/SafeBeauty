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
    [InlineData("Titanium Dioxide [Nano]", "TITANIUM DIOXIDE (NANO)")]
    [InlineData("Alcohol Denat.", "ALCOHOL DENAT")]
    [InlineData("Coco-Caprylate Caprate", "COCO-CAPRYLATE/CAPRATE")]
    [InlineData("Coco-Caprylate/Caprate", "COCO-CAPRYLATE/CAPRATE")]
    [InlineData("Lanolin Alcohol (Eucerit®)", "LANOLIN ALCOHOL")]
    [InlineData("Lanolin Alcohol (Example™)", "LANOLIN ALCOHOL")]
    [InlineData("AQUA / WATER / EAU", "AQUA")]
    [InlineData("AQUA/WATER/EAU", "AQUA")]
    [InlineData("WATER(AQUA/EAU)", "AQUA")]
    [InlineData("COPERNICIA CERIFERA CERA / CARNAUBA WAX / CIRE DE CARNAUBA", "COPERNICIA CERIFERA CERA")]
    [InlineData("BUTYROSPERMUM PARKII BUTTER / SHEA BUTTER", "BUTYROSPERMUM PARKII BUTTER")]
    [InlineData("XANTHAN GUM (F.I.L. N70032039/1).", "XANTHAN GUM")]
    [InlineData("VITIS VINIFERA (GRAPE) SEED OIL", "VITIS VINIFERA SEED OIL")]
    [InlineData("HELIANTHUS ANNUUS (SUNFLOWER) SEED OIL", "HELIANTHUS ANNUUS SEED OIL")]
    [InlineData("HELIANTHUS ANNUUS (SUNFLOWER) SEED OIL (HELIANTHUS ANNUUS SEED OIL)", "HELIANTHUS ANNUUS SEED OIL")]
    [InlineData("Water (Aqua)", "AQUA")]
    [InlineData(@"Water\Aqua\Eau", "AQUA")]
    [InlineData("Psidium Guajava (Guava) Fruit Extract", "PSIDIUM GUAJAVA FRUIT EXTRACT")]
    [InlineData("Gentiana Lutea (Gentian) Root Extract", "GENTIANA LUTEA ROOT EXTRACT")]
    [InlineData(@"Hordeum Vulgare (Barley) Extract\Extrait D'Orge", "HORDEUM VULGARE EXTRACT")]
    [InlineData("Triticum Vulgare (Wheat) Germ Extract", "TRITICUM VULGARE GERM EXTRACT")]
    [InlineData("Rosmarinus Officinalis (Rosemary) Leaf Extract", "ROSMARINUS OFFICINALIS LEAF EXTRACT")]
    [InlineData("Elaeis Guineensis (Palm) Kernel Oil", "ELAEIS GUINEENSIS KERNEL OIL")]
    [InlineData("Yellow 5 (CI 19140)", "CI 19140")]
    [InlineData("Blue 1 (CI 42090)", "CI 42090")]
    [InlineData("Chromium Hydroxide Green *(CI 77289)*", "CI 77289")]
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
