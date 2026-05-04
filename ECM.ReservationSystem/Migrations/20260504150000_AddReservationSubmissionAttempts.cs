using System;
using ECM.ReservationSystem.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECM.ReservationSystem.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260504150000_AddReservationSubmissionAttempts")]
    public partial class AddReservationSubmissionAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReservationSubmissionAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Outcome = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReservationId = table.Column<int>(type: "int", nullable: true),
                    DocumentId = table.Column<long>(type: "bigint", nullable: true),
                    WorkflowId = table.Column<long>(type: "bigint", nullable: true),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EmployeeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Sector = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    CheckInDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckOutDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NumberOfGuests = table.Column<int>(type: "int", nullable: false),
                    IsTransportationRequired = table.Column<bool>(type: "bit", nullable: false),
                    PaymentReceiptNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InsuranceReceiptNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CaseSystemId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestPayload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponsePayload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationSubmissionAttempts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReservationSubmissionAttempts_DocumentId",
                table: "ReservationSubmissionAttempts",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationSubmissionAttempts_UnitId_CheckInDate_CheckOutDate",
                table: "ReservationSubmissionAttempts",
                columns: new[] { "UnitId", "CheckInDate", "CheckOutDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReservationSubmissionAttempts");
        }
    }
}
