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

            migrationBuilder.CreateTable(
                name: "PersonNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AzureUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(2500)", maxLength: 2500, nullable: true),
                    IsShared = table.Column<bool>(type: "bit", nullable: false),
                    Updated = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonNotes_Persons_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonNotes_AzureUniqueId",
                table: "PersonNotes",
                column: "AzureUniqueId")
                .Annotation("SqlServer:Clustered", false)
                .Annotation("SqlServer:Include", new[] { "Id", "Title", "Content", "IsShared", "Updated", "UpdatedById" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonNotes_UpdatedById",
                table: "PersonNotes",
                column: "UpdatedById");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonNotes");

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
