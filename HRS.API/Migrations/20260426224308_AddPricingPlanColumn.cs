using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingPlanColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PricingPlan",
                table: "Reservations",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PricingPlan",
                table: "Reservations");
        }
    }
}
