using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeBeauty.API.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueNormalizedIngredientNameIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_NormalizedInciName",
                table: "Ingredients",
                column: "NormalizedInciName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Ingredients_NormalizedInciName",
                table: "Ingredients");
        }
    }
}
