using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECM.ReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexingforReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_DocumentId",
                table: "Reservations",
                column: "DocumentId",
                filter: "[DocumentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_UnitId_CheckInDate_CheckOutDate",
                table: "Reservations",
                columns: new[] { "UnitId", "CheckInDate", "CheckOutDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservations_DocumentId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_UnitId_CheckInDate_CheckOutDate",
                table: "Reservations");

        }
    }
}
