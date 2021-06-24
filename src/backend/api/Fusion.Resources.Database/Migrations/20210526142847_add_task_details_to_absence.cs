using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fusion.Resources.Database.Migrations
{
    public partial class add_task_details_to_absence : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPrivate",
                table: "PersonAbsences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "TaskDetails_BasePositionId",
                table: "PersonAbsences",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaskDetails_Location",
                table: "PersonAbsences",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaskDetails_RoleName",
                table: "PersonAbsences",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaskDetails_TaskName",
                table: "PersonAbsences",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPrivate",
                table: "PersonAbsences");

            migrationBuilder.DropColumn(
                name: "TaskDetails_BasePositionId",
                table: "PersonAbsences");

            migrationBuilder.DropColumn(
                name: "TaskDetails_Location",
                table: "PersonAbsences");

            migrationBuilder.DropColumn(
                name: "TaskDetails_RoleName",
                table: "PersonAbsences");

            migrationBuilder.DropColumn(
                name: "TaskDetails_TaskName",
                table: "PersonAbsences");
        }
    }
}
