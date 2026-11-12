using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymRoomsGG.Migrations
{
    /// <inheritdoc />
    public partial class AddGymOperatingTimeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "ClosingTime",
                table: "GymInfos",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "OpeningTime",
                table: "GymInfos",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClosingTime",
                table: "GymInfos");

            migrationBuilder.DropColumn(
                name: "OpeningTime",
                table: "GymInfos");
        }
    }
}
