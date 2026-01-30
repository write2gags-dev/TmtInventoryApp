using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TmtInventoryApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPredefinedBundleWeightToTmtVariant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "PredefinedBundleWeight",
                table: "TmtVariants",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 1,
                column: "PredefinedBundleWeight",
                value: 0.0);

            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 2,
                column: "PredefinedBundleWeight",
                value: 0.0);

            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 3,
                column: "PredefinedBundleWeight",
                value: 0.0);

            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 4,
                column: "PredefinedBundleWeight",
                value: 0.0);

            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 5,
                column: "PredefinedBundleWeight",
                value: 0.0);

            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 7,
                column: "PredefinedBundleWeight",
                value: 0.0);

            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 8,
                column: "PredefinedBundleWeight",
                value: 0.0);

            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 9,
                column: "PredefinedBundleWeight",
                value: 0.0);

            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 10,
                column: "PredefinedBundleWeight",
                value: 0.0);

            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 11,
                column: "PredefinedBundleWeight",
                value: 0.0);

            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 12,
                column: "PredefinedBundleWeight",
                value: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PredefinedBundleWeight",
                table: "TmtVariants");
        }
    }
}
