using SafeBeauty.API.Models;

namespace SafeBeauty.API.Tests;

public class IngredientListParserTests
{
    [Fact]
    public void Parse_SplitsCommonCosmeticLabelSeparators()
    {
        var entries = new[]
        {
            "AQUA • GLYCERIN, TOCOPHEROL; XANTHAN GUM\nCITRIC ACID"
        };

        var result = IngredientListParser.Parse(entries);

        Assert.Equal(
            ["AQUA", "GLYCERIN", "TOCOPHEROL", "XANTHAN GUM", "CITRIC ACID"],
            result);
    }

    [Fact]
    public void Parse_PreservesSlashesInsideInciNamesAndSynonyms()
    {
        var entries = new[]
        {
            "AQUA / WATER / EAU • ACRYLATES/C10-30 ALKYL ACRYLATE CROSSPOLYMER"
        };

        var result = IngredientListParser.Parse(entries);

        Assert.Equal(
            ["AQUA / WATER / EAU", "ACRYLATES/C10-30 ALKYL ACRYLATE CROSSPOLYMER"],
            result);
    }
}
