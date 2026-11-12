using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymRoomsGG.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainerQuestStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "TrainerQuests");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "TrainerQuests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "TrainerQuests",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "TrainerQuests");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "TrainerQuests");

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "TrainerQuests",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
