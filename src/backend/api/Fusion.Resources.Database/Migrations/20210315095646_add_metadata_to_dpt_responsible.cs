using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fusion.Resources.Database.Migrations
{
    public partial class add_metadata_to_dpt_responsible : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "DateTo",
                table: "DepartmentResponsibles",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "DateFrom",
                table: "DepartmentResponsibles",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DateCreated",
                table: "DepartmentResponsibles",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "getutcdate()");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DateUpdated",
                table: "DepartmentResponsibles",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "getutcdate()");

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "DepartmentResponsibles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "DepartmentResponsibles",
                keyColumn: "Id",
                keyValue: new Guid("20621fbc-dc4e-4958-95c9-2ac56e166973"),
                columns: new[] { "DateFrom", "DateTo" },
                values: new object[] { new DateTimeOffset(new DateTime(2020, 12, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 1, 0, 0, 0)), new DateTimeOffset(new DateTime(2021, 12, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 1, 0, 0, 0)) });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateCreated",
                table: "DepartmentResponsibles");

            migrationBuilder.DropColumn(
                name: "DateUpdated",
                table: "DepartmentResponsibles");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "DepartmentResponsibles");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateTo",
                table: "DepartmentResponsibles",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateFrom",
                table: "DepartmentResponsibles",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.UpdateData(
                table: "DepartmentResponsibles",
                keyColumn: "Id",
                keyValue: new Guid("20621fbc-dc4e-4958-95c9-2ac56e166973"),
                columns: new[] { "DateFrom", "DateTo" },
                values: new object[] { new DateTime(2020, 12, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2021, 12, 31, 0, 0, 0, 0, DateTimeKind.Unspecified) });
        }
    }
}
