using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECM.ReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class SplitTicketPoolsByBookingType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Keep the existing single pool as the Employee pool (default type), and add a new Pension pool.
            migrationBuilder.RenameColumn(
                name: "TicketCount",
                table: "TripLocations",
                newName: "EmployeeTicketCount");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "Tickets",
                newName: "EmployeeQuantity");

            migrationBuilder.AddColumn<int>(
                name: "PensionTicketCount",
                table: "TripLocations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PensionQuantity",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PensionTicketCount",
                table: "TripLocations");

            migrationBuilder.DropColumn(
                name: "PensionQuantity",
                table: "Tickets");

            migrationBuilder.RenameColumn(
                name: "EmployeeTicketCount",
                table: "TripLocations",
                newName: "TicketCount");

            migrationBuilder.RenameColumn(
                name: "EmployeeQuantity",
                table: "Tickets",
                newName: "Quantity");
        }
    }
}
