using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECM.ReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddGenericAuditAndMoveEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CreatedByUserId",
                table: "UnitTypes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "UnitTypes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "UpdatedByUserId",
                table: "UnitTypes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CreatedByUserId",
                table: "Units",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Units",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "UpdatedByUserId",
                table: "Units",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CreatedByUserId",
                table: "TransportationCosts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "TransportationCosts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "UpdatedByUserId",
                table: "TransportationCosts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CreatedByUserId",
                table: "Reservations",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "UpdatedByUserId",
                table: "Reservations",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CreatedByUserId",
                table: "Pricings",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Pricings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "UpdatedByUserId",
                table: "Pricings",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CreatedByUserId",
                table: "Cities",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Cities",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "UpdatedByUserId",
                table: "Cities",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CreatedByUserId",
                table: "AvailableDates",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "AvailableDates",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "UpdatedByUserId",
                table: "AvailableDates",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "UnitTypes");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "UnitTypes");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "UnitTypes");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "TransportationCosts");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "TransportationCosts");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "TransportationCosts");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Pricings");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Pricings");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Pricings");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "AvailableDates");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AvailableDates");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "AvailableDates");
        }
    }
}
