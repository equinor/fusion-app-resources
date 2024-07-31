using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fusion.Summary.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    DepartmentSapId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ResourceOwnerAzureUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullDepartmentName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.DepartmentSapId);
                });

            migrationBuilder.CreateTable(
                name: "WeeklySummaryReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentSapId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Period = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NumberOfPersonnel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CapacityInUse = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NumberOfRequestsLastPeriod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NumberOfOpenRequests = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NumberOfRequestsStartingInLessThanThreeMonths = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NumberOfRequestsStartingInMoreThanThreeMonths = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AverageTimeToHandleRequests = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AllocationChangesAwaitingTaskOwnerAction = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProjectChangesAffectingNextThreeMonths = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EndingPositions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PersonnelMoreThan100PercentFTEs = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklySummaryReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeeklySummaryReports_Departments_DepartmentSapId",
                        column: x => x.DepartmentSapId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentSapId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeeklySummaryReports_DepartmentSapId_Period",
                table: "WeeklySummaryReports",
                columns: new[] { "DepartmentSapId", "Period" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeeklySummaryReports");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
