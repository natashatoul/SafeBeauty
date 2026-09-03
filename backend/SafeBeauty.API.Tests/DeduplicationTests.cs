using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SafeBeauty.API.Models;
using SafeBeauty.API.Models.Enums;
using SafeBeauty.API.Services;

namespace SafeBeauty.API.Tests;

public class DeduplicationTests
{
    [Fact]
    public async Task RunAsync_MergesDuplicates_AndKeepsCategoriesAndWorstRating()
    {
        // Integration
        // Arrange(prepear) - Act - Assert(check)
        // Reproduce two historical rows from different official sources.
        // Arrange
        using var database = new TestDatabase();
        var context = database.Context;

        var glossaryCategory = new IngredientCategory
        {
            Name = "EU Glossary Ingredient"
        };
        var restrictedCategory = new IngredientCategory
        {
            Name = "Restricted Substance"
        };

        var glossaryRetinol = new Ingredient
        {
            InciName = "RETINOL",
            NormalizedInciName = string.Empty,
            SafetyRating = SafetyRating.Grey,
            Source = "EU Glossary"
        };
        glossaryRetinol.CategoryMappings.Add(new IngredientCategoryMapping
        {
            Category = glossaryCategory,
            MappingType = "RegulatoryGlossary",
            Source = "EU Glossary",
            Notes = "Glossary classification"
        });

        var annexRetinol = new Ingredient
        {
            InciName = "Retinol",
            NormalizedInciName = string.Empty,
            SafetyRating = SafetyRating.Amber,
            Source = "Annex III"
        };
        annexRetinol.CategoryMappings.Add(new IngredientCategoryMapping
        {
            Category = restrictedCategory,
            MappingType = "RegulatoryAnnexNormalizedV3",
            Source = "Annex III",
            Notes = "Restricted substance"
        });

        context.Ingredients.AddRange(glossaryRetinol, annexRetinol);
        await context.SaveChangesAsync();

        var service = new IngredientDeduplicationService(
            context,
            NullLogger<IngredientDeduplicationService>.Instance);

        // Act
        // method in the IngredientDeduplicationService
        await service.RunAsync();

        context.ChangeTracker.Clear();
        var remaining = await context.Ingredients
            .Include(ingredient => ingredient.CategoryMappings)
            .SingleAsync();

        // Assert
        Assert.Equal("RETINOL", remaining.InciName);
        Assert.Equal("RETINOL", remaining.NormalizedInciName);
        Assert.Equal(SafetyRating.Amber, remaining.SafetyRating);
        Assert.Equal(2, remaining.CategoryMappings.Count);
    }

    [Fact]
    public async Task RunAsync_CanRunTwice_WithoutChangingCleanedData()
    {
        // Arrange
        using var database = new TestDatabase();
        var context = database.Context;

        context.Ingredients.AddRange(
            new Ingredient
            {
                InciName = "RETINOL",
                SafetyRating = SafetyRating.Grey
            },
            new Ingredient
            {
                InciName = " Retinol ",
                SafetyRating = SafetyRating.Amber
            });
        await context.SaveChangesAsync();

        var service = new IngredientDeduplicationService(
            context,
            NullLogger<IngredientDeduplicationService>.Instance);

        // Act: the second run must be a safe no-op.
        await service.RunAsync();
        await service.RunAsync();

        context.ChangeTracker.Clear();
        var ingredients = await context.Ingredients.ToListAsync();

        // Assert
        var survivor = Assert.Single(ingredients);
        Assert.Equal("RETINOL", survivor.NormalizedInciName);
        Assert.Equal(SafetyRating.Amber, survivor.SafetyRating);
    }

    [Fact]
    public async Task RunAsync_PrefersDuplicateWithFunctionMetadata()
    {
        // CosIng may contain a synonym-like row without functions and
        // a later canonical row with useful function metadata.
        using var database = new TestDatabase();
        var context = database.Context;

        context.Ingredients.AddRange(
            new Ingredient
            {
                InciName = "CERA MICROCRISTALLINA",
                SafetyRating = SafetyRating.Grey
            },
            new Ingredient
            {
                InciName = "MICROCRYSTALLINE WAX",
                Function = "BINDING, BULKING, EMULSION STABILISING, VISCOSITY CONTROLLING",
                SafetyRating = SafetyRating.Grey
            });
        await context.SaveChangesAsync();

        var service = new IngredientDeduplicationService(
            context,
            NullLogger<IngredientDeduplicationService>.Instance);

        // Act
        await service.RunAsync();

        context.ChangeTracker.Clear();
        var survivor = await context.Ingredients.SingleAsync();

        // Assert
        Assert.Equal("MICROCRYSTALLINE WAX", survivor.InciName);
        Assert.Equal("MICROCRYSTALLINE WAX", survivor.NormalizedInciName);
        Assert.Contains("VISCOSITY CONTROLLING", survivor.Function);
    }

    [Fact]
    public async Task RunAsync_PreservesDistinctRestrictions_FromTheSameAnnex()
    {
        
        using var database = new TestDatabase();
        var context = database.Context;

        var first = new Ingredient
        {
            InciName = "RETINOL",
            SafetyRating = SafetyRating.Grey
        };
        first.AnnexRestrictions.Add(new AnnexRestriction
        {
            AnnexType = AnnexType.III,
            MaxConcentration = "0.05%",
            ProductType = "Body lotion",
            Detail = "First restriction"
        });

        var second = new Ingredient
        {
            InciName = "Retinol",
            SafetyRating = SafetyRating.Amber
        };
        second.AnnexRestrictions.Add(new AnnexRestriction
        {
            AnnexType = AnnexType.III,
            MaxConcentration = "0.3%",
            ProductType = "Other products",
            Detail = "Second restriction"
        });

        context.Ingredients.AddRange(first, second);
        await context.SaveChangesAsync();

        var service = new IngredientDeduplicationService(
            context,
            NullLogger<IngredientDeduplicationService>.Instance);

        // Act
        await service.RunAsync();

        context.ChangeTracker.Clear();
        var survivor = await context.Ingredients
            .Include(ingredient => ingredient.AnnexRestrictions)
            .SingleAsync();

        // Assert: same AnnexType does not mean the restrictions are duplicates.
        Assert.Equal(2, survivor.AnnexRestrictions.Count);
        Assert.Contains(
            survivor.AnnexRestrictions,
            restriction => restriction.MaxConcentration == "0.05%");
        Assert.Contains(
            survivor.AnnexRestrictions,
            restriction => restriction.MaxConcentration == "0.3%");
    }

    [Fact]
    public async Task UniqueIndex_RejectsDuplicateNormalizedInciName()
    {
        // Clean the historical duplicates before applying the index.
        using var database = new TestDatabase();
        var context = database.Context;

        context.Ingredients.Add(new Ingredient
        {
            InciName = "RETINOL",
            NormalizedInciName = "RETINOL",
            SafetyRating = SafetyRating.Amber
        });
        await context.SaveChangesAsync();

        var service = new IngredientDeduplicationService(
            context,
            NullLogger<IngredientDeduplicationService>.Instance);
        await service.RunAsync();

        database.ApplyRemainingMigrations();

        context.Ingredients.Add(new Ingredient
        {
            InciName = "Retinol",
            NormalizedInciName = "RETINOL",
            SafetyRating = SafetyRating.Grey
        });

        // Act + Assert: the database, not only application code, blocks it.
        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());
    }
}
