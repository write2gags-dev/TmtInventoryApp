using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TmtInventoryApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TmtVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Diameter = table.Column<int>(type: "INTEGER", nullable: false),
                    WeightPerMeter = table.Column<double>(type: "REAL", nullable: false),
                    StandardLength = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TmtVariants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TmtVariantId = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantityBundles = table.Column<int>(type: "INTEGER", nullable: false),
                    PiecesPerBundle = table.Column<int>(type: "INTEGER", nullable: false),
                    LoosePieces = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockEntries_TmtVariants_TmtVariantId",
                        column: x => x.TmtVariantId,
                        principalTable: "TmtVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "TmtVariants",
                columns: new[] { "Id", "Diameter", "StandardLength", "WeightPerMeter" },
                values: new object[,]
                {
                    { 1, 8, 12.0, 0.39500000000000002 },
                    { 2, 10, 12.0, 0.61699999999999999 },
                    { 3, 12, 12.0, 0.88800000000000001 },
                    { 4, 16, 12.0, 1.5800000000000001 },
                    { 5, 20, 12.0, 2.4700000000000002 },
                    { 6, 25, 12.0, 3.8500000000000001 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockEntries_TmtVariantId",
                table: "StockEntries",
                column: "TmtVariantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockEntries");

            migrationBuilder.DropTable(
                name: "TmtVariants");
        }
    }
}
