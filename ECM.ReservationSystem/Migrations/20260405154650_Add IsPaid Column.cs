using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECM.ReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPaidColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "UnitScheduleSlots",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "UnitScheduleSlots");
        }
    }
}
