using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECM.ReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitSchedulesTransportationAndPensioners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsForPensioners",
                table: "Units",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "TransportationCostPerPerson",
                table: "Pricings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "UnitScheduleSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SlotStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SlotEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitScheduleSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnitScheduleSlots_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnitScheduleSlots_UnitId_SlotStartDate_SlotEndDate",
                table: "UnitScheduleSlots",
                columns: new[] { "UnitId", "SlotStartDate", "SlotEndDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnitScheduleSlots");

            migrationBuilder.DropColumn(
                name: "IsForPensioners",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "TransportationCostPerPerson",
                table: "Pricings");
        }
    }
}
