using System.Reflection;
using SafeBeauty.API.DTOs;
using SafeBeauty.API.Services;

namespace SafeBeauty.API.Tests;

public class AiSummaryPromptTests
{
    [Fact]
    public void BuildMessages_DoesNotExposeUnmatchedIngredientNames()
    {
        var response = new AnalyseResponse
        {
            Results =
            [
                new IngredientResultDto
                {
                    InciName = "GLYCERIN",
                    SafetyRating = "Green",
                    Category = "Humectants",
                    Function = "HUMECTANT",
                    ConditionFlags =
                    [
                        new ConditionFlagDto
                        {
                            Condition = "AtopicDermatitis",
                            FlagType = "Beneficial",
                            Notes = "MEDICAL EVIDENCE NOTE MUST NOT REACH THE MODEL"
                        }
                    ]
                }
            ],
            UnknownIngredients =
            [
                new AiIngredientResultDto
                {
                    Name = "WWW EXAMPLE COM 89057",
                    AiLabel = "Unknown",
                    Confidence = 0
                }
            ]
        };

        var method = typeof(AiSummaryService).GetMethod(
            "BuildMessages",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
        var messages = ((string SystemMessage, string UserMessage))method!.Invoke(
            null,
            [response, new List<string>()])!;

        Assert.DoesNotContain("WWW EXAMPLE COM 89057", messages.SystemMessage);
        Assert.DoesNotContain("WWW EXAMPLE COM 89057", messages.UserMessage);
        Assert.Contains("1 unmatched", messages.UserMessage);
        Assert.Contains("Raw unmatched names are not provided", messages.UserMessage);
        Assert.DoesNotContain("MEDICAL EVIDENCE NOTE", messages.UserMessage);
        Assert.Contains("category-level profile rule", messages.UserMessage);
        Assert.DoesNotContain("AtopicDermatitis", messages.UserMessage);
        Assert.Contains(
            "Only ingredients explicitly listed under 'Beneficial profile signals'",
            messages.SystemMessage);
    }

    [Theory]
    [InlineData("This product may be suitable for eczema.")]
    [InlineData("It is a suitable option for the selected profile.")]
    [InlineData("The formula is designed to soothe skin.")]
    [InlineData("These ingredients are beneficial for atopic dermatitis.")]
    public void ViolatesSafetyBoundary_RejectsMedicalOrSuitabilityClaims(string summary)
    {
        var method = typeof(AiSummaryService).GetMethod(
            "ViolatesSafetyBoundary",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
        Assert.True((bool)method!.Invoke(null, [summary])!);
    }

    [Fact]
    public void ContradictsProfileFlags_RejectsClaimThatAvoidFlagsAreAbsent()
    {
        var response = new AnalyseResponse
        {
            Results =
            [
                new IngredientResultDto
                {
                    InciName = "PARFUM",
                    ConditionFlags =
                    [
                        new ConditionFlagDto { FlagType = "Avoid" }
                    ]
                }
            ]
        };
        var method = typeof(AiSummaryService).GetMethod(
            "ContradictsProfileFlags",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
        Assert.True((bool)method!.Invoke(
            null,
            ["No ingredients were specifically flagged as Avoid for the selected profile.", response])!);
    }

    [Fact]
    public void ContradictsProfileFlags_RejectsConcernWhenNoAvoidFlagsExist()
    {
        var method = typeof(AiSummaryService).GetMethod(
            "ContradictsProfileFlags",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
        Assert.True((bool)method!.Invoke(
            null,
            ["An ingredient is flagged as a potential concern.", new AnalyseResponse()])!);
    }

    [Fact]
    public void ContradictsProfileFlags_RejectsBeneficialClaimTransferredToUnflaggedIngredients()
    {
        var response = new AnalyseResponse
        {
            Results =
            [
                new IngredientResultDto
                {
                    InciName = "ISOPROPYL PALMITATE",
                    ConditionFlags =
                    [
                        new ConditionFlagDto { FlagType = "Beneficial" }
                    ]
                },
                new IngredientResultDto
                {
                    InciName = "ETHYLHEXYL TRIAZONE",
                    IsUvFilter = true,
                    UvFilterType = "Organic"
                }
            ]
        };
        var method = typeof(AiSummaryService).GetMethod(
            "ContradictsProfileFlags",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
        Assert.True((bool)method!.Invoke(
            null,
            ["ETHYLHEXYL TRIAZONE is a UV filter flagged as beneficial in the cosmetic rule set.", response])!);
    }

    [Fact]
    public void ContradictsProfileFlags_AllowsBeneficialClaimForExplicitlyFlaggedIngredient()
    {
        var response = new AnalyseResponse
        {
            Results =
            [
                new IngredientResultDto
                {
                    InciName = "ISOPROPYL PALMITATE",
                    ConditionFlags =
                    [
                        new ConditionFlagDto { FlagType = "Beneficial" }
                    ]
                }
            ]
        };
        var method = typeof(AiSummaryService).GetMethod(
            "ContradictsProfileFlags",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
        Assert.False((bool)method!.Invoke(
            null,
            ["ISOPROPYL PALMITATE is flagged as beneficial in the cosmetic rule set.", response])!);
    }

    [Fact]
    public void BuildFallbackSummary_ExplainsUvLimitsAndUsesSingularConcernGrammar()
    {
        var response = new AnalyseResponse
        {
            Results =
            [
                new IngredientResultDto
                {
                    InciName = "ETHYLHEXYL TRIAZONE",
                    IsUvFilter = true,
                    UvFilterType = "Organic"
                },
                new IngredientResultDto
                {
                    InciName = "PARFUM",
                    ConditionFlags =
                    [
                        new ConditionFlagDto { FlagType = "Avoid" }
                    ]
                }
            ]
        };
        var method = typeof(AiSummaryService).GetMethod(
            "BuildFallbackSummary",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
        var summary = (string)method!.Invoke(null, [response])!;

        Assert.Contains("cannot confirm the finished product's SPF", summary);
        Assert.Contains("PARFUM was specifically flagged", summary);
        Assert.DoesNotContain("PARFUM were", summary);
    }
}
