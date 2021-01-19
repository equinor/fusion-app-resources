using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fusion.Resources.Database.Migrations
{
    public partial class Addproposedpersonwasnotified : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Created",
                table: "ResourceAllocationRequests",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(2021, 1, 19, 9, 32, 15, 152, DateTimeKind.Unspecified).AddTicks(8511), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldDefaultValue: new DateTimeOffset(new DateTime(2021, 1, 19, 7, 31, 45, 128, DateTimeKind.Unspecified).AddTicks(86), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<bool>(
                name: "WasNotified",
                table: "Persons",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WasNotified",
                table: "Persons");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Created",
                table: "ResourceAllocationRequests",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(2021, 1, 19, 7, 31, 45, 128, DateTimeKind.Unspecified).AddTicks(86), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldDefaultValue: new DateTimeOffset(new DateTime(2021, 1, 19, 9, 32, 15, 152, DateTimeKind.Unspecified).AddTicks(8511), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
