using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TmtInventoryApp.Migrations
{
    /// <inheritdoc />
    public partial class AddUsePredefinedBundleWeight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UsePredefinedBundleWeight",
                table: "TmtVariants",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 1,
                column: "UsePredefinedBundleWeight",
                value: false);

            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 2,
                column: "UsePredefinedBundleWeight",
                value: false);

            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 3,
                column: "UsePredefinedBundleWeight",
                value: false);

            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 4,
                column: "UsePredefinedBundleWeight",
                value: false);

            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 5,
                column: "UsePredefinedBundleWeight",
                value: false);

            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 7,
                column: "UsePredefinedBundleWeight",
                value: false);

            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 8,
                column: "UsePredefinedBundleWeight",
                value: false);

            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 9,
                column: "UsePredefinedBundleWeight",
                value: false);

            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 10,
                column: "UsePredefinedBundleWeight",
                value: false);

            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 11,
                column: "UsePredefinedBundleWeight",
                value: false);

            migrationBuilder.UpdateData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 12,
                column: "UsePredefinedBundleWeight",
                value: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsePredefinedBundleWeight",
                table: "TmtVariants");
        }
    }
}
