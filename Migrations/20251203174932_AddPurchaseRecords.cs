using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TmtInventoryApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PurchaseRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PurchaseDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    VehicleNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseRecordItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PurchaseRecordId = table.Column<int>(type: "INTEGER", nullable: false),
                    TmtVariantId = table.Column<int>(type: "INTEGER", nullable: false),
                    Bundles = table.Column<int>(type: "INTEGER", nullable: false),
                    WeightTons = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseRecordItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseRecordItems_PurchaseRecords_PurchaseRecordId",
                        column: x => x.PurchaseRecordId,
                        principalTable: "PurchaseRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseRecordItems_TmtVariants_TmtVariantId",
                        column: x => x.TmtVariantId,
                        principalTable: "TmtVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRecordItems_PurchaseRecordId",
                table: "PurchaseRecordItems",
                column: "PurchaseRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRecordItems_TmtVariantId",
                table: "PurchaseRecordItems",
                column: "TmtVariantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchaseRecordItems");

            migrationBuilder.DropTable(
                name: "PurchaseRecords");
        }
    }
}
