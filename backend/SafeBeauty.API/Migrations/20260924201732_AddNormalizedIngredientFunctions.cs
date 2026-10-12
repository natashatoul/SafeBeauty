using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeBeauty.API.Migrations
{
    /// <inheritdoc />
    public partial class AddNormalizedIngredientFunctions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CosmeticFunctions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    SupportsSkin = table.Column<bool>(type: "INTEGER", nullable: false),
                    SupportsFormula = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CosmeticFunctions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IngredientFunctions",
                columns: table => new
                {
                    IngredientId = table.Column<int>(type: "INTEGER", nullable: false),
                    CosmeticFunctionId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngredientFunctions", x => new { x.IngredientId, x.CosmeticFunctionId });
                    table.ForeignKey(
                        name: "FK_IngredientFunctions_CosmeticFunctions_CosmeticFunctionId",
                        column: x => x.CosmeticFunctionId,
                        principalTable: "CosmeticFunctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IngredientFunctions_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CosmeticFunctions_Name",
                table: "CosmeticFunctions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IngredientFunctions_CosmeticFunctionId",
                table: "IngredientFunctions",
                column: "CosmeticFunctionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IngredientFunctions");

            migrationBuilder.DropTable(
                name: "CosmeticFunctions");
        }
    }
}
