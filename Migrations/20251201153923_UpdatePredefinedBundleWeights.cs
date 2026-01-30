using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TmtInventoryApp.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePredefinedBundleWeights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update predefined bundle weights for TMT bars by diameter
            migrationBuilder.Sql("UPDATE TmtVariants SET PredefinedBundleWeight = 54.990 WHERE Diameter = 8");
            migrationBuilder.Sql("UPDATE TmtVariants SET PredefinedBundleWeight = 57.100 WHERE Diameter = 10");
            migrationBuilder.Sql("UPDATE TmtVariants SET PredefinedBundleWeight = 62.500 WHERE Diameter = 12");
            migrationBuilder.Sql("UPDATE TmtVariants SET PredefinedBundleWeight = 55.560 WHERE Diameter = 16");
            migrationBuilder.Sql("UPDATE TmtVariants SET PredefinedBundleWeight = 59.250 WHERE Diameter = 20");
            
            // Update predefined bundle weights for rings by name
            migrationBuilder.Sql("UPDATE TmtVariants SET PredefinedBundleWeight = 8.087 WHERE Name LIKE '%7/7%' AND Name LIKE '%ring%'");
            migrationBuilder.Sql("UPDATE TmtVariants SET PredefinedBundleWeight = 7.040 WHERE Name LIKE '%7/4%' AND Name LIKE '%ring%'");
            migrationBuilder.Sql("UPDATE TmtVariants SET PredefinedBundleWeight = 8.080 WHERE Name LIKE '%7/9%' AND Name LIKE '%ring%'");
            
            // Update predefined bundle weight for wire (default to 25kg, can be edited to 10kg as needed)
            migrationBuilder.Sql("UPDATE TmtVariants SET PredefinedBundleWeight = 25.000 WHERE Name LIKE '%wire%'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
