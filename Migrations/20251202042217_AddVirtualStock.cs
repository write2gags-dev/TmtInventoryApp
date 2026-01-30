using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TmtInventoryApp.Migrations
{
    /// <inheritdoc />
    public partial class AddVirtualStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VirtualStocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TmtVariantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AvailableWeightKg = table.Column<decimal>(type: "TEXT", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VirtualStocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VirtualStocks_TmtVariants_TmtVariantId",
                        column: x => x.TmtVariantId,
                        principalTable: "TmtVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VirtualStockTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VirtualStockId = table.Column<int>(type: "INTEGER", nullable: false),
                    TransactionType = table.Column<int>(type: "INTEGER", nullable: false),
                    WeightKg = table.Column<decimal>(type: "TEXT", nullable: false),
                    ReferenceType = table.Column<string>(type: "TEXT", nullable: true),
                    ReferenceId = table.Column<int>(type: "INTEGER", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VirtualStockTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VirtualStockTransactions_VirtualStocks_VirtualStockId",
                        column: x => x.VirtualStockId,
                        principalTable: "VirtualStocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VirtualStocks_TmtVariantId",
                table: "VirtualStocks",
                column: "TmtVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_VirtualStockTransactions_VirtualStockId",
                table: "VirtualStockTransactions",
                column: "VirtualStockId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VirtualStockTransactions");

            migrationBuilder.DropTable(
                name: "VirtualStocks");
        }
    }
}
