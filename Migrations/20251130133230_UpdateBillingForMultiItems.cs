using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TmtInventoryApp.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBillingForMultiItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillingRecords_TmtVariants_TmtVariantId",
                table: "BillingRecords");

            migrationBuilder.DropIndex(
                name: "IX_BillingRecords_TmtVariantId",
                table: "BillingRecords");

            migrationBuilder.DropColumn(
                name: "BilledWeightKg",
                table: "BillingRecords");

            migrationBuilder.DropColumn(
                name: "TmtVariantId",
                table: "BillingRecords");

            migrationBuilder.CreateTable(
                name: "BillingRecordItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BillingRecordId = table.Column<int>(type: "INTEGER", nullable: false),
                    TmtVariantId = table.Column<int>(type: "INTEGER", nullable: false),
                    BilledWeightKg = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingRecordItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingRecordItems_BillingRecords_BillingRecordId",
                        column: x => x.BillingRecordId,
                        principalTable: "BillingRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BillingRecordItems_TmtVariants_TmtVariantId",
                        column: x => x.TmtVariantId,
                        principalTable: "TmtVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillingRecordItems_BillingRecordId",
                table: "BillingRecordItems",
                column: "BillingRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingRecordItems_TmtVariantId",
                table: "BillingRecordItems",
                column: "TmtVariantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillingRecordItems");

            migrationBuilder.AddColumn<decimal>(
                name: "BilledWeightKg",
                table: "BillingRecords",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TmtVariantId",
                table: "BillingRecords",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_BillingRecords_TmtVariantId",
                table: "BillingRecords",
                column: "TmtVariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_BillingRecords_TmtVariants_TmtVariantId",
                table: "BillingRecords",
                column: "TmtVariantId",
                principalTable: "TmtVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
