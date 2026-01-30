using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TmtInventoryApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesItemWeight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "WeightKg",
                table: "SalesOrderItems",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WeightKg",
                table: "SalesOrderItems");
        }
    }
}
