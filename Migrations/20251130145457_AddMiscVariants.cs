using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TmtInventoryApp.Migrations
{
    /// <inheritdoc />
    public partial class AddMiscVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "TmtVariants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: null);

            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: null);

            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: null);

            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 4,
                column: "Name",
                value: null);

            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 5,
                column: "Name",
                value: null);

            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 6,
                column: "Name",
                value: null);

            migrationBuilder.InsertData(
                table: "TmtVariants",
                columns: new[] { "Id", "Diameter", "Name", "StandardLength", "WeightPerMeter" },
                values: new object[,]
                {
                    { 7, 0, "Wire - 20 Gauge", 12.0, 0.0 },
                    { 8, 0, "Nail (bAG) - 2 Inch", 12.0, 0.0 },
                    { 9, 0, "Nail (bAG) - 2.5 Inch", 12.0, 0.0 },
                    { 10, 0, "Ring - 7/7", 12.0, 0.0 },
                    { 11, 0, "Ring - 7/4", 12.0, 0.0 },
                    { 12, 0, "Ring - 7/9", 12.0, 0.0 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DropColumn(
                name: "Name",
                table: "TmtVariants");
        }
    }
}
