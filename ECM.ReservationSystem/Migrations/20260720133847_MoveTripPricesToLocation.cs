using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECM.ReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class MoveTripPricesToLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Add the price columns to TripLocations (default 0).
            migrationBuilder.AddColumn<decimal>(
                name: "AdultTicketPrice",
                table: "TripLocations",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ChildTicketPrice",
                table: "TripLocations",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CompanionTicketPrice",
                table: "TripLocations",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            // 2) Preserve existing data: copy prices from each location's most recent trip
            //    into the location. Locations without trips keep the default 0.
            migrationBuilder.Sql(@"
                UPDATE tl
                SET tl.AdultTicketPrice = t.AdultTicketPrice,
                    tl.ChildTicketPrice = t.ChildTicketPrice,
                    tl.CompanionTicketPrice = t.CompanionTicketPrice
                FROM TripLocations tl
                CROSS APPLY (
                    SELECT TOP 1 AdultTicketPrice, ChildTicketPrice, CompanionTicketPrice
                    FROM Trips
                    WHERE Trips.TripLocationId = tl.Id
                    ORDER BY Trips.Id DESC
                ) t;");

            // 3) Drop the price columns from Trips.
            migrationBuilder.DropColumn(
                name: "AdultTicketPrice",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "ChildTicketPrice",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "CompanionTicketPrice",
                table: "Trips");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1) Re-add the price columns on Trips (default 0).
            migrationBuilder.AddColumn<decimal>(
                name: "AdultTicketPrice",
                table: "Trips",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ChildTicketPrice",
                table: "Trips",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CompanionTicketPrice",
                table: "Trips",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            // 2) Copy each location's prices back onto all of its trips.
            migrationBuilder.Sql(@"
                UPDATE t
                SET t.AdultTicketPrice = tl.AdultTicketPrice,
                    t.ChildTicketPrice = tl.ChildTicketPrice,
                    t.CompanionTicketPrice = tl.CompanionTicketPrice
                FROM Trips t
                INNER JOIN TripLocations tl ON tl.Id = t.TripLocationId;");

            // 3) Drop the price columns from TripLocations.
            migrationBuilder.DropColumn(
                name: "AdultTicketPrice",
                table: "TripLocations");

            migrationBuilder.DropColumn(
                name: "ChildTicketPrice",
                table: "TripLocations");

            migrationBuilder.DropColumn(
                name: "CompanionTicketPrice",
                table: "TripLocations");
        }
    }
}
