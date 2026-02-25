using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECM.ReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class InitialEdit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Capacity",
                table: "Units",
                newName: "Year");

            migrationBuilder.RenameColumn(
                name: "WeeklyRent",
                table: "Pricings",
                newName: "WeeklyRentDefaultCapacity");

            migrationBuilder.AddColumn<int>(
                name: "DefaultCapacity",
                table: "Units",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FloorNumber",
                table: "Units",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FloorType",
                table: "Units",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxCapacity",
                table: "Units",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "AdditionalPersonCost",
                table: "Pricings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "FloorType",
                table: "Pricings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultCapacity",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "FloorNumber",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "FloorType",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "MaxCapacity",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "AdditionalPersonCost",
                table: "Pricings");

            migrationBuilder.DropColumn(
                name: "FloorType",
                table: "Pricings");

            migrationBuilder.RenameColumn(
                name: "Year",
                table: "Units",
                newName: "Capacity");

            migrationBuilder.RenameColumn(
                name: "WeeklyRentDefaultCapacity",
                table: "Pricings",
                newName: "WeeklyRent");
        }
    }
}
