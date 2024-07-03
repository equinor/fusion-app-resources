using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fusion.Summary.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class SummaryReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SummaryReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentSapId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PeriodType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Period = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NumberOfPersonnel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CapacityInUse = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NumberOfRequestsLastPeriod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NumberOfOpenRequests = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NumberOfRequestsStartingInLessThanThreeMonths = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NumberOfRequestsStartingInMoreThanThreeMonths = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AverageTimeToHandleRequests = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AllocationChangesAwaitingTaskOwnerAction = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProjectChangesAffectingNextThreeMonths = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SummaryReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EndingPositions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FTE = table.Column<int>(type: "int", nullable: false),
                    SummaryReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EndingPositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EndingPositions_SummaryReports_SummaryReportId",
                        column: x => x.SummaryReportId,
                        principalTable: "SummaryReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonnelMoreThan100PercentFTEs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SummaryReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonnelMoreThan100PercentFTEs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonnelMoreThan100PercentFTEs_SummaryReports_SummaryReportId",
                        column: x => x.SummaryReportId,
                        principalTable: "SummaryReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EndingPositions_SummaryReportId",
                table: "EndingPositions",
                column: "SummaryReportId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonnelMoreThan100PercentFTEs_SummaryReportId",
                table: "PersonnelMoreThan100PercentFTEs",
                column: "SummaryReportId");

            migrationBuilder.CreateIndex(
                name: "IX_SummaryReports_DepartmentSapId_PeriodType_Period",
                table: "SummaryReports",
                columns: new[] { "DepartmentSapId", "PeriodType", "Period" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EndingPositions");

            migrationBuilder.DropTable(
                name: "PersonnelMoreThan100PercentFTEs");

            migrationBuilder.DropTable(
                name: "SummaryReports");
        }
    }
}
