using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECM.ReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddTripTicketsCompanionAndPaymentHold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TripBookings_TotalGuests_Max",
                table: "TripBookings");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Trips");

            migrationBuilder.AddColumn<decimal>(
                name: "CompanionTicketPrice",
                table: "Trips",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TicketCount",
                table: "TripLocations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "CompanionUnitPrice",
                table: "TripBookings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CompanionsCount",
                table: "TripBookings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDeadline",
                table: "TripBookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_TripBookings_CompanionsCount_NonNegative",
                table: "TripBookings",
                sql: "[CompanionsCount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TripBookings_TotalGuests_Max",
                table: "TripBookings",
                sql: "[AdultsCount] + [ChildrenCount] + [CompanionsCount] <= 5");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TripBookings_CompanionsCount_NonNegative",
                table: "TripBookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TripBookings_TotalGuests_Max",
                table: "TripBookings");

            migrationBuilder.DropColumn(
                name: "CompanionTicketPrice",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "TicketCount",
                table: "TripLocations");

            migrationBuilder.DropColumn(
                name: "CompanionUnitPrice",
                table: "TripBookings");

            migrationBuilder.DropColumn(
                name: "CompanionsCount",
                table: "TripBookings");

            migrationBuilder.DropColumn(
                name: "PaymentDeadline",
                table: "TripBookings");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Trips",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_TripBookings_TotalGuests_Max",
                table: "TripBookings",
                sql: "[AdultsCount] + [ChildrenCount] <= 5");
        }
    }
}
