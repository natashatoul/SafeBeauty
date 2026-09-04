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
        Assert.Equal("Amber", result.SafetyRating);
        Assert.Contains("Skin Conditioning", result.Category);
        var conditionFlag = Assert.Single(result.ConditionFlags);
        Assert.Equal("Acne", conditionFlag.Condition);

        var unknown = Assert.Single(response.UnknownIngredients);
        Assert.Equal("SOME UNKNOWN INGREDIENT", unknown.Name);
        Assert.Equal("Unknown", unknown.AiLabel);
        Assert.Equal(0, unknown.Confidence);
        Assert.False(string.IsNullOrWhiteSpace(response.AiSummary));
    }
}