using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fusion.Summary.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class Add_TaskOwnerReports_Table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OrgProjectExternalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DirectorAzureUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedAdminsAzureUniqueId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeeklyTaskOwnerReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActionsAwaitingTaskOwnerAction = table.Column<int>(type: "int", nullable: false),
                    AdminAccessExpiringInLessThanThreeMonths = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PositionAllocationsEndingInNextThreeMonths = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TBNPositionsStartingInLessThanThreeMonths = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyTaskOwnerReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeeklyTaskOwnerReports_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OrgProjectExternalId",
                table: "Projects",
                column: "OrgProjectExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyTaskOwnerReports_ProjectId_PeriodStart_PeriodEnd",
                table: "WeeklyTaskOwnerReports",
                columns: new[] { "ProjectId", "PeriodStart", "PeriodEnd" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeeklyTaskOwnerReports");

            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
