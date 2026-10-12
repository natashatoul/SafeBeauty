using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SafeBeauty.API.Models;
using SafeBeauty.API.Models.Enums;
using SafeBeauty.API.Services;

namespace SafeBeauty.API.Tests;

public class AnalyseAsyncTests
{
    [Fact]
    public async Task AnalyseAsync_EnrichesKnownIngredients_AndHandlesUnknownIngredients()
    {
        using var database = new TestDatabase();
        var context = database.Context;

        var category = new IngredientCategory
        {
            Name = "Skin Conditioning"
        };
        category.ConditionRules.Add(new ConditionRule
        {
            Condition = Condition.Acne,
            FlagType = FlagType.Caution,
            Notes = "Check individual tolerance.",
            EvidenceSource = "Test source"
        });

        var retinol = new Ingredient
        {
            InciName = "RETINOL",
            NormalizedInciName = "RETINOL",
            SafetyRating = SafetyRating.Amber,
            Function = "SKIN CONDITIONING"
        };
        retinol.CategoryMappings.Add(new IngredientCategoryMapping
        {
            Category = category
        });

        context.Ingredients.Add(retinol);
        await context.SaveChangesAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HuggingFace:ApiKey"] = "",
                ["HuggingFace:LlmApiKey"] = ""
            })
            .Build();

        using var httpClient = new HttpClient();
        var huggingFace = new HuggingFaceService(
            httpClient,
            configuration,
            NullLogger<HuggingFaceService>.Instance);
        var aiSummary = new AiSummaryService(
            httpClient,
            configuration,
            NullLogger<AiSummaryService>.Instance);
        var service = new IngredientAnalysisService(context, huggingFace, aiSummary);

        var response = await service.AnalyseAsync(
            ["RETINOL", "SOME UNKNOWN INGREDIENT"],
            ["Acne"]);

        var result = Assert.Single(response.Results);
        Assert.Equal("RETINOL", result.InciName);
        Assert.Equal(0, result.InputOrder);
        Assert.Equal("Amber", result.SafetyRating);
        Assert.Contains("Skin Conditioning", result.Category);
        var cosmeticFunction = Assert.Single(result.Functions);
        Assert.Equal("SKIN CONDITIONING", cosmeticFunction.Name);
        Assert.False(string.IsNullOrWhiteSpace(cosmeticFunction.Description));
        Assert.True(cosmeticFunction.SupportsSkin);
        Assert.False(cosmeticFunction.SupportsFormula);
        var conditionFlag = Assert.Single(result.ConditionFlags);
        Assert.Equal("Acne", conditionFlag.Condition);

        var unknown = Assert.Single(response.UnknownIngredients);
        Assert.Equal("SOME UNKNOWN INGREDIENT", unknown.Name);
        Assert.Equal(1, unknown.InputOrder);
        Assert.Equal("Unknown", unknown.AiLabel);
        Assert.Equal(0, unknown.Confidence);
        Assert.Equal("Unavailable", unknown.ClassificationStatus);
        Assert.False(string.IsNullOrWhiteSpace(response.AiSummary));
    }

    [Fact]
    public async Task AnalyseAsync_MarksAiClassificationAsSkipped_WhenUnknownLimitIsExceeded()
    {
        using var database = new TestDatabase();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HuggingFace:ApiKey"] = "",
                ["HuggingFace:LlmApiKey"] = ""
            })
            .Build();

        using var httpClient = new HttpClient();
        var service = new IngredientAnalysisService(
            database.Context,
            new HuggingFaceService(
                httpClient,
                configuration,
                NullLogger<HuggingFaceService>.Instance),
            new AiSummaryService(
                httpClient,
                configuration,
                NullLogger<AiSummaryService>.Instance));

        var response = await service.AnalyseAsync(
            ["UNKNOWN 1", "UNKNOWN 2", "UNKNOWN 3", "UNKNOWN 4", "UNKNOWN 5", "UNKNOWN 6"]);

        Assert.Equal(6, response.UnknownIngredients.Count);
        Assert.All(response.UnknownIngredients, ingredient =>
            Assert.Equal("SkippedLimit", ingredient.ClassificationStatus));
    }
}
