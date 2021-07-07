using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fusion.Resources.Database.Migrations
{
    public partial class add_request_tasks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RequestTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    SubType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Responsible = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolvedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PropertiesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestTasks_Persons_ResolvedById",
                        column: x => x.ResolvedById,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestTasks_ResourceAllocationRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "ResourceAllocationRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequestTasks_RequestId",
                table: "RequestTasks",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTasks_ResolvedById",
                table: "RequestTasks",
                column: "ResolvedById");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestTasks");

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
