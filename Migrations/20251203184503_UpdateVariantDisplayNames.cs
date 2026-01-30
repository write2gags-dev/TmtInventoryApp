using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TmtInventoryApp.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVariantDisplayNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 8,
                column: "Name",
                value: "Nail (BAG) - 2 Inch");

            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 9,
                column: "Name",
                value: "Nail (BAG) - 2.5 Inch");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 8,
                column: "Name",
                value: "Nail (bAG) - 2 Inch");

            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 9,
                column: "Name",
                value: "Nail (bAG) - 2.5 Inch");
        }
    }
}
