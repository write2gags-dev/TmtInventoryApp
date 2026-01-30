using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TmtInventoryApp.Migrations
{
    /// <inheritdoc />
    public partial class Remove25mmVariant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "TmtVariants",
                keyColumn: "Id",
                keyValue: 6);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "TmtVariants",
                columns: new[] { "Id", "Diameter", "Name", "StandardLength", "WeightPerMeter" },
                values: new object[] { 6, 25, null, 12.0, 3.8500000000000001 });
        }
    }
}
