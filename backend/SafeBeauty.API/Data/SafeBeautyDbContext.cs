using Microsoft.EntityFrameworkCore;
using SafeBeauty.API.Models;


namespace SafeBeauty.API.Data;

public class SafeBeautyDbContext : DbContext
{
    public SafeBeautyDbContext(DbContextOptions<SafeBeautyDbContext> options)
        : base(options) { }

    public DbSet<Ingredient> Ingredients { get; set; }
    public DbSet<IngredientCategory> IngredientCategories { get; set; }
    public DbSet<IngredientSynonym> IngredientSynonyms { get; set; }
    public DbSet<AnnexRestriction> AnnexRestrictions { get; set; }
    public DbSet<ConditionRule> ConditionRules { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<ScanHistory> ScanHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ingredient>()
            .HasOne(i => i.Category)
            .WithMany(c => c.Ingredients)
            .HasForeignKey(i => i.CategoryId);

        modelBuilder.Entity<ConditionRule>()
            .HasOne(cr => cr.Category)
            .WithMany(c => c.ConditionRules)
            .HasForeignKey(cr => cr.CategoryId);
    }

}
