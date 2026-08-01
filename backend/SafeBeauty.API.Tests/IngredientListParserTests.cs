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
    public void Parse_SplitsMiddleDotSeparatedAsianLabelList()
    {
        var result = IngredientListParser.Parse([
            "DIMETHICONE･WATER(AQUA/EAU)･DIISOPROPYL SEBACATE"
        ]);

        Assert.Equal(["DIMETHICONE", "WATER(AQUA/EAU)", "DIISOPROPYL SEBACATE"], result);
    }

    [Fact]
    public void Parse_PreservesNumericCommaInsideChemicalName()
    {
        var result = IngredientListParser.Parse([
            "Oil,1,2-Hexanediol,Tocopherol"
        ]);

        Assert.Equal(["Oil", "1,2-Hexanediol", "Tocopherol"], result);
    }

    [Fact]
    public void Parse_PreservesBotanicalSuffixSplitByCopiedLineBreak()
    {
        var result = IngredientListParser.Parse([
            "Simmondsia Chinensis (Jojoba) Seed\nOil,1,2-Hexanediol"
        ]);

        Assert.Equal(["Simmondsia Chinensis (Jojoba) Seed Oil", "1,2-Hexanediol"], result);
    }

    [Fact]
    public void Parse_JoinsKnownOcrFragmentsFromBarcodeSource()
    {
        var result = IngredientListParser.Parse([
            "C20-40 PA,RETH-10,SODIUM HYDROX,DE,TRIETHA,NOLAMINE,PARAF,FINUM LIQUIDUM"
        ]);

        Assert.Equal(
            ["C20-40 PARETH-10", "SODIUM HYDROXIDE", "TRIETHANOLAMINE", "PARAFFINUM LIQUIDUM"],
            result);
    }

    [Fact]
    public void Parse_RepairsStructuredBarcodeFragmentsAndRemovesFilMetadata()
    {
        var result = IngredientListParser.Parse([
            "SHEA BUTTER",
            "ACRYLATES",
            "C10-30 ALKYL ACRYLATE CROSSPOLYMER",
            "F.I.L",
            "Z70028645"
        ]);

        Assert.Equal(
            ["SHEA BUTTER", "ACRYLATES/C10-30 ALKYL ACRYLATE CROSSPOLYMER"],
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
