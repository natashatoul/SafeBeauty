using Microsoft.EntityFrameworkCore;
using SafeBeauty.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;


namespace SafeBeauty.API.Data;

public class SafeBeautyDbContext : IdentityDbContext<IdentityUser>
{
    public SafeBeautyDbContext(DbContextOptions<SafeBeautyDbContext> options)
        : base(options) { }

    public DbSet<Ingredient> Ingredients { get; set; }
    public DbSet<IngredientCategory> IngredientCategories { get; set; }
    public DbSet<IngredientCategoryMapping> IngredientCategoryMappings { get; set; }
    public DbSet<IngredientSynonym> IngredientSynonyms { get; set; }
    public DbSet<AnnexRestriction> AnnexRestrictions { get; set; }
    public DbSet<ConditionRule> ConditionRules { get; set; }
    public DbSet<ScanHistory> ScanHistories { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    
    {
        base.OnModelCreating(modelBuilder); // set up tables (AspNetUsers, 
        // AspNetRoles и т.д.) excactly in this DB. 
        // If do not call base this tables will set up not correct.
        modelBuilder.Entity<Ingredient>()
            .HasIndex(i => i.NormalizedInciName)
            .IsUnique();

        modelBuilder.Entity<IngredientCategoryMapping>()
            .HasKey(m => new { m.IngredientId, m.CategoryId });

        modelBuilder.Entity<IngredientCategoryMapping>()
            .HasOne(m => m.Ingredient)
            .WithMany(i => i.CategoryMappings)
            .HasForeignKey(m => m.IngredientId);

        modelBuilder.Entity<IngredientCategoryMapping>()
            .HasOne(m => m.Category)
            .WithMany(c => c.IngredientMappings)
            .HasForeignKey(m => m.CategoryId);

        modelBuilder.Entity<ConditionRule>()
            .HasOne(cr => cr.Category)
            .WithMany(c => c.ConditionRules)
            .HasForeignKey(cr => cr.CategoryId);
    }

}
