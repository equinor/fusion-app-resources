using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fusion.Resources.Database.Migrations
{
    public partial class seed_departments_data : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "DepartmentResponsibles",
                columns: new[] { "Id", "DateFrom", "DateTo", "DepartmentId", "ResponsibleAzureObjectId" },
                values: new object[] { new Guid("20621fbc-dc4e-4958-95c9-2ac56e166973"), new DateTime(2020, 12, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2021, 12, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), "TPD PRD PMC PCA PCA7", new Guid("20621fbc-dc4e-4958-95c9-2ac56e166973") });

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "DepartmentId", "SectorId" },
                values: new object[,]
                {
                    { "TPD PRD PMC PCA PCA1", "TPD PRD PMC PCA" },
                    { "TPD PRD PMC PCA", "TPD PRD PMC PCA" },
                    { "TPD PRD PMC CCH CM4", "TPD PRD PMC CCH" },
                    { "TPD PRD PMC CCH CM3", "TPD PRD PMC CCH" },
                    { "TPD PRD PMC CCH CM2", "TPD PRD PMC CCH" },
                    { "TPD PRD PMC CCH CM1", "TPD PRD PMC CCH" },
                    { "TPD PRD PMC CCH CHU3", "TPD PRD PMC CCH" },
                    { "TPD PRD PMC CCH CHU2", "TPD PRD PMC CCH" },
                    { "TPD PRD PMC CCH CHU1", "TPD PRD PMC CCH" },
                    { "TPD PRD PMC CCH", "TPD PRD PMC CCH" },
                    { "TPD PRD FE SP SP6", "TPD PRD FE SP" },
                    { "TPD PRD FE SP SP5", "TPD PRD FE SP" },
                    { "TPD PRD FE SP SP4", "TPD PRD FE SP" },
                    { "TPD PRD FE SP SP3", "TPD PRD FE SP" },
                    { "TPD PRD FE SP SP2", "TPD PRD FE SP" },
                    { "TPD PRD PMC PCA PCA2", "TPD PRD PMC PCA" },
                    { "TPD PRD FE SP SP1", "TPD PRD FE SP" },
                    { "TPD PRD PMC PCA PCA3", "TPD PRD PMC PCA" },
                    { "TPD PRD PMC PCA PCA5", "TPD PRD PMC PCA" },
                    { "TPD PRD PMC QA QRM2", "TPD PRD PMC QA" },
                    { "TPD PRD PMC QA QRM1", "TPD PRD PMC QA" },
                    { "TPD PRD PMC QA IDM1", "TPD PRD PMC QA" },
                    { "TPD PRD PMC QA DM2", "TPD PRD PMC QA" },
                    { "TPD PRD PMC QA ADM3", "TPD PRD PMC QA" },
                    { "TPD PRD PMC QA ADM2", "TPD PRD PMC QA" },
                    { "TPD PRD PMC QA ADM1", "TPD PRD PMC QA" },
                    { "TPD PRD PMC QA", "TPD PRD PMC QA" },
                    { "TPD PRD PMC PM PM4", "TPD PRD PMC PM" },
                    { "TPD PRD PMC PM PM3", "TPD PRD PMC PM" },
                    { "TPD PRD PMC PM PM2", "TPD PRD PMC PM" },
                    { "TPD PRD PMC PM PM1", "TPD PRD PMC PM" },
                    { "TPD PRD PMC PM", "TPD PRD PMC PM" },
                    { "TPD PRD PMC PCA PCA7", "TPD PRD PMC PCA" },
                    { "TPD PRD PMC PCA PCA6", "TPD PRD PMC PCA" },
                    { "TPD PRD PMC PCA PCA4", "TPD PRD PMC PCA" },
                    { "TPD PRD PMC QA RES", "TPD PRD PMC QA" },
                    { "TPD PRD FE SP", "TPD PRD FE SP" },
                    { "TPD PRD FE SE TS", "TPD PRD FE SE" },
                    { "TPD PRD FE MMS MAT1", "TPD PRD FE MMS" },
                    { "TPD PRD FE MMS", "TPD PRD FE MMS" },
                    { "TPD PRD FE EM EM5", "TPD PRD FE EM" }
                });

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "DepartmentId", "SectorId" },
                values: new object[,]
                {
                    { "TPD PRD FE EM EM4", "TPD PRD FE EM" },
                    { "TPD PRD FE EM EM3", "TPD PRD FE EM" },
                    { "TPD PRD FE EM EM2", "TPD PRD FE EM" },
                    { "TPD PRD FE EM EM1", "TPD PRD FE EM" },
                    { "TPD PRD FE EM", "TPD PRD FE EM" },
                    { "TPD PRD FE EA EA5", "TPD PRD FE EA" },
                    { "TPD PRD FE EA EA4 CON", "TPD PRD FE EA" },
                    { "TPD PRD FE EA EA4", "TPD PRD FE EA" },
                    { "TPD PRD FE EA EA3 CON", "TPD PRD FE EA" },
                    { "TPD PRD FE EA EA3", "TPD PRD FE EA" },
                    { "TPD PRD FE EA EA2", "TPD PRD FE EA" },
                    { "TPD PRD FE EA EA1", "TPD PRD FE EA" },
                    { "TPD PRD FE MMS MAT2", "TPD PRD FE MMS" },
                    { "TPD PRD FE SE TWE", "TPD PRD FE SE" },
                    { "TPD PRD FE MMS MEC1", "TPD PRD FE MMS" },
                    { "TPD PRD FE MMS MEC3", "TPD PRD FE MMS" },
                    { "TPD PRD FE SE TDS", "TPD PRD FE SE" },
                    { "TPD PRD FE SE SUS", "TPD PRD FE SE" },
                    { "TPD PRD FE SE PR2", "TPD PRD FE SE" },
                    { "TPD PRD FE SE PR1", "TPD PRD FE SE" },
                    { "TPD PRD FE SE FA", "TPD PRD FE SE" },
                    { "TPD PRD FE SE", "TPD PRD FE SE" },
                    { "TPD PRD FE MO MAR2", "TPD PRD FE MO" },
                    { "TPD PRD FE MO MAR1", "TPD PRD FE MO" },
                    { "TPD PRD FE MO MAP", "TPD PRD FE MO" },
                    { "TPD PRD FE MO GMS", "TPD PRD FE MO" },
                    { "TPD PRD FE MO GEO", "TPD PRD FE MO" },
                    { "TPD PRD FE MO", "TPD PRD FE MO" },
                    { "TPD PRD FE MMS STR2", "TPD PRD FE MMS" },
                    { "TPD PRD FE MMS STR1", "TPD PRD FE MMS" },
                    { "TPD PRD FE MMS MEC4", "TPD PRD FE MMS" },
                    { "TPD PRD FE MMS MEC2", "TPD PRD FE MMS" },
                    { "TPD PRD FE EA", "TPD PRD FE EA" }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DepartmentResponsibles",
                keyColumn: "Id",
                keyValue: new Guid("20621fbc-dc4e-4958-95c9-2ac56e166973"));

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE EA");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE EA EA1");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE EA EA2");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE EA EA3");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE EA EA3 CON");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE EA EA4");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE EA EA4 CON");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE EA EA5");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE EM");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE EM EM1");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE EM EM2");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE EM EM3");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE EM EM4");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE EM EM5");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE MMS");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE MMS MAT1");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE MMS MAT2");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE MMS MEC1");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE MMS MEC2");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE MMS MEC3");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE MMS MEC4");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE MMS STR1");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE MMS STR2");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE MO");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE MO GEO");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE MO GMS");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE MO MAP");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE MO MAR1");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE MO MAR2");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE SE");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE SE FA");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE SE PR1");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE SE PR2");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE SE SUS");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE SE TDS");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE SE TS");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE SE TWE");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE SP");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE SP SP1");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE SP SP2");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE SP SP3");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE SP SP4");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE SP SP5");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD FE SP SP6");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD PMC CCH");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD PMC CCH CHU1");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD PMC CCH CHU2");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD PMC CCH CHU3");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD PMC CCH CM1");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD PMC CCH CM2");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD PMC CCH CM3");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD PMC CCH CM4");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD PMC PCA");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD PMC PCA PCA1");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD PMC PCA PCA2");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD PMC PCA PCA3");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD PMC PCA PCA4");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD PMC PCA PCA5");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD PMC PCA PCA6");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD PMC PCA PCA7");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD PMC PM");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD PMC PM PM1");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD PMC PM PM2");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD PMC PM PM3");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD PMC PM PM4");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD PMC QA");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD PMC QA ADM1");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD PMC QA ADM2");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD PMC QA ADM3");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD PMC QA DM2");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD PMC QA IDM1");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD PMC QA QRM1");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD PMC QA QRM2");

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: "TPD PRD PMC QA RES");
        }
    }
}
