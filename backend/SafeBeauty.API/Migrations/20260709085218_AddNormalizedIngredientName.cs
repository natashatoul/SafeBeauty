using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeBeauty.API.Migrations
{
    /// <inheritdoc />
    public partial class AddNormalizedIngredientName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedInciName",
                table: "Ingredients",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NormalizedInciName",
                table: "Ingredients");
        }
    }
}
