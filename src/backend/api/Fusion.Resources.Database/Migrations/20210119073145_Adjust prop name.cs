using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fusion.Resources.Database.Migrations
{
    public partial class Adjustpropname : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ResourceAllocationOrgPositionInstance_Workload",
                table: "ResourceAllocationRequests",
                newName: "OrgPositionInstance_Workload");

            migrationBuilder.RenameColumn(
                name: "ResourceAllocationOrgPositionInstance_Obs",
                table: "ResourceAllocationRequests",
                newName: "OrgPositionInstance_Obs");

            migrationBuilder.RenameColumn(
                name: "ResourceAllocationOrgPositionInstance_Location",
                table: "ResourceAllocationRequests",
                newName: "OrgPositionInstance_Location");

            migrationBuilder.RenameColumn(
                name: "ResourceAllocationOrgPositionInstance_Id",
                table: "ResourceAllocationRequests",
                newName: "OrgPositionInstance_Id");

            migrationBuilder.RenameColumn(
                name: "ResourceAllocationOrgPositionInstance_AppliesTo",
                table: "ResourceAllocationRequests",
                newName: "OrgPositionInstance_AppliesTo");

            migrationBuilder.RenameColumn(
                name: "ResourceAllocationOrgPositionInstance_AppliesFrom",
                table: "ResourceAllocationRequests",
                newName: "OrgPositionInstance_AppliesFrom");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Created",
                table: "ResourceAllocationRequests",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(2021, 1, 19, 7, 31, 45, 128, DateTimeKind.Unspecified).AddTicks(86), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldDefaultValue: new DateTimeOffset(new DateTime(2021, 1, 14, 15, 30, 28, 655, DateTimeKind.Unspecified).AddTicks(2798), new TimeSpan(0, 0, 0, 0, 0)));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OrgPositionInstance_Workload",
                table: "ResourceAllocationRequests",
                newName: "ResourceAllocationOrgPositionInstance_Workload");

            migrationBuilder.RenameColumn(
                name: "OrgPositionInstance_Obs",
                table: "ResourceAllocationRequests",
                newName: "ResourceAllocationOrgPositionInstance_Obs");

            migrationBuilder.RenameColumn(
                name: "OrgPositionInstance_Location",
                table: "ResourceAllocationRequests",
                newName: "ResourceAllocationOrgPositionInstance_Location");

            migrationBuilder.RenameColumn(
                name: "OrgPositionInstance_Id",
                table: "ResourceAllocationRequests",
                newName: "ResourceAllocationOrgPositionInstance_Id");

            migrationBuilder.RenameColumn(
                name: "OrgPositionInstance_AppliesTo",
                table: "ResourceAllocationRequests",
                newName: "ResourceAllocationOrgPositionInstance_AppliesTo");

            migrationBuilder.RenameColumn(
                name: "OrgPositionInstance_AppliesFrom",
                table: "ResourceAllocationRequests",
                newName: "ResourceAllocationOrgPositionInstance_AppliesFrom");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Created",
                table: "ResourceAllocationRequests",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(2021, 1, 14, 15, 30, 28, 655, DateTimeKind.Unspecified).AddTicks(2798), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldDefaultValue: new DateTimeOffset(new DateTime(2021, 1, 19, 7, 31, 45, 128, DateTimeKind.Unspecified).AddTicks(86), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
