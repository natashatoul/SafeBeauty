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

    [Fact]
    public void Parse_SplitsFullStopSeparatedWebsiteList()
    {
        var result = IngredientListParser.Parse([
            "Zinc Oxide [Nano]. Avene Thermal Spring Water. Titanium Dioxide [Nano]."
        ]);

        Assert.Equal(
            ["Zinc Oxide [Nano]", "Avene Thermal Spring Water", "Titanium Dioxide [Nano]."],
            result);
    }

    [Fact]
    public void Parse_PreservesDotsInsideFilReference()
    {
        var result = IngredientListParser.Parse([
            "Aqua. Xanthan Gum (F.I.L. N70032039/1)."
        ]);

        Assert.Equal(["Aqua", "Xanthan Gum (F.I.L. N70032039/1)."], result);
    }

    [Fact]
    public void SegmentByKnownNames_UsesVocabularyForListWithoutSeparators()
    {
        const string entry =
            "AQUA / WATER ALCOHOL DENAT DIISOPROPYL SEBACATE ETHYLHEXYL TRIAZONE " +
            "BIS-ETHYLHEXYLOXYPHENOL METHOXYPHENYL TRIAZINE GLYCERIN";
        var knownNames = new[]
        {
            "AQUA",
            "ALCOHOL DENAT",
            "DIISOPROPYL SEBACATE",
            "ETHYLHEXYL TRIAZONE",
            "BIS-ETHYLHEXYLOXYPHENOL METHOXYPHENYL TRIAZINE",
            "GLYCERIN"
        };

        var result = IngredientListParser.SegmentByKnownNames(entry, knownNames);

        Assert.Equal(knownNames, result);
    }
}
