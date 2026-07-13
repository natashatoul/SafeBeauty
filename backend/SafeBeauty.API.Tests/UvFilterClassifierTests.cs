using SafeBeauty.API.Models;
using SafeBeauty.API.Services;

namespace SafeBeauty.API.Tests;

public class UvFilterClassifierTests
{
    [Theory]
    [InlineData("ZINC OXIDE", "Mineral / inorganic")]
    [InlineData("TITANIUM DIOXIDE (NANO)", "Mineral / inorganic")]
    [InlineData("METHYLENE BIS-BENZOTRIAZOLYL TETRAMETHYLBUTYLPHENOL", "Organic particulate")]
    [InlineData("BUTYL METHOXYDIBENZOYLMETHANE", "Organic")]
    public void Classify_ReturnsConsumerFacingFilterType(string inciName, string expected)
    {
        Assert.Equal(expected, UvFilterClassifier.Classify(inciName));
    }

    [Fact]
    public void IsConfirmedAnnexViMapping_RejectsLegacyFalsePositive()
    {
        var category = new IngredientCategory { Name = "UV Filter" };
        var legacyMapping = new IngredientCategoryMapping
        {
            Category = category,
            Source = UvFilterClassifier.AnnexSource,
            MappingType = "RegulatoryAnnex"
        };
        var currentMapping = new IngredientCategoryMapping
        {
            Category = category,
            Source = UvFilterClassifier.AnnexSource,
            MappingType = UvFilterClassifier.CurrentMappingType
        };

        Assert.False(UvFilterClassifier.IsConfirmedAnnexViMapping(legacyMapping));
        Assert.True(UvFilterClassifier.IsConfirmedAnnexViMapping(currentMapping));
    }
}
