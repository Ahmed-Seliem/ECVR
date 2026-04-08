using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECM.ReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitFacadesAndRoomCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RoomCount",
                table: "Units",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UnitFacadeId",
                table: "Units",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UnitFacades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitFacades", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Units_UnitFacadeId",
                table: "Units",
                column: "UnitFacadeId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitFacades_Name",
                table: "UnitFacades",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Units_UnitFacades_UnitFacadeId",
                table: "Units",
                column: "UnitFacadeId",
                principalTable: "UnitFacades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Units_UnitFacades_UnitFacadeId",
                table: "Units");

            migrationBuilder.DropTable(
                name: "UnitFacades");

            migrationBuilder.DropIndex(
                name: "IX_Units_UnitFacadeId",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "RoomCount",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "UnitFacadeId",
                table: "Units");
        }
    }
}
