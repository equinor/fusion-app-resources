using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fusion.Resources.Database.Migrations
{
    public partial class Addedworkflow : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Workflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    LogicAppName = table.Column<string>(nullable: true),
                    LogicAppVersion = table.Column<string>(nullable: true),
                    State = table.Column<string>(nullable: false),
                    SystemMessage = table.Column<string>(nullable: true),
                    RequestId = table.Column<Guid>(nullable: false),
                    RequestType = table.Column<int>(nullable: false),
                    Created = table.Column<DateTimeOffset>(nullable: false),
                    Completed = table.Column<DateTimeOffset>(nullable: true),
                    TerminatedbyId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workflows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workflows_Persons_TerminatedbyId",
                        column: x => x.TerminatedbyId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DbWorkflowStep",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    WorkflowId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    Reason = table.Column<string>(nullable: true),
                    CompletedById = table.Column<Guid>(nullable: true),
                    State = table.Column<string>(nullable: false),
                    Started = table.Column<DateTimeOffset>(nullable: true),
                    Completed = table.Column<DateTimeOffset>(nullable: true),
                    DueDate = table.Column<DateTimeOffset>(nullable: true),
                    PreviousStep = table.Column<string>(nullable: true),
                    NextStep = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbWorkflowStep", x => new { x.Id, x.WorkflowId });
                    table.ForeignKey(
                        name: "FK_DbWorkflowStep_Persons_CompletedById",
                        column: x => x.CompletedById,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DbWorkflowStep_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DbWorkflowStep_CompletedById",
                table: "DbWorkflowStep",
                column: "CompletedById");

            migrationBuilder.CreateIndex(
                name: "IX_DbWorkflowStep_WorkflowId",
                table: "DbWorkflowStep",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_TerminatedbyId",
                table: "Workflows",
                column: "TerminatedbyId");

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_RequestId_RequestType",
                table: "Workflows",
                columns: new[] { "RequestId", "RequestType" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DbWorkflowStep");

            migrationBuilder.DropTable(
                name: "Workflows");
        }
    }
}
