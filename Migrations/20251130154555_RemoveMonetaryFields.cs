using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TmtInventoryApp.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMonetaryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "SubTotal",
                table: "SalesOrderItems");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "SalesOrderItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "SalesOrders",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SubTotal",
                table: "SalesOrderItems",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "SalesOrderItems",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
