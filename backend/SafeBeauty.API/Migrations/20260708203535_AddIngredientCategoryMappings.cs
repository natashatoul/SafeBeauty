using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeBeauty.API.Migrations
{
    /// <inheritdoc />
    public partial class AddIngredientCategoryMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IngredientCategoryMappings",
                columns: table => new
                {
                    IngredientId = table.Column<int>(type: "INTEGER", nullable: false),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    MappingType = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngredientCategoryMappings", x => new { x.IngredientId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_IngredientCategoryMappings_IngredientCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "IngredientCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IngredientCategoryMappings_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IngredientCategoryMappings_CategoryId",
                table: "IngredientCategoryMappings",
                column: "CategoryId");

            migrationBuilder.Sql(
                """
                INSERT INTO IngredientCategoryMappings
                    (IngredientId, CategoryId, MappingType, Source, Notes)
                SELECT
                    Id,
                    CategoryId,
                    'LegacyCategory',
                    'Migration',
                    'Migrated from Ingredients.CategoryId'
                FROM Ingredients;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Ingredients_IngredientCategories_CategoryId",
                table: "Ingredients");

            migrationBuilder.DropIndex(
                name: "IX_Ingredients_CategoryId",
                table: "Ingredients");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Ingredients");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Ingredients",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE Ingredients
                SET CategoryId = (
                    SELECT CategoryId
                    FROM IngredientCategoryMappings
                    WHERE IngredientCategoryMappings.IngredientId = Ingredients.Id
                    ORDER BY CategoryId
                    LIMIT 1
                );
                """);

            migrationBuilder.DropTable(
                name: "IngredientCategoryMappings");

            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_CategoryId",
                table: "Ingredients",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ingredients_IngredientCategories_CategoryId",
                table: "Ingredients",
                column: "CategoryId",
                principalTable: "IngredientCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
