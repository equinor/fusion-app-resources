using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fusion.Resources.Database.Migrations
{
    public partial class Cleanup_OrgPositionInstance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrgPositionInstance_AppliesFrom",
                table: "ResourceAllocationRequests");

            migrationBuilder.DropColumn(
                name: "OrgPositionInstance_AppliesTo",
                table: "ResourceAllocationRequests");

            migrationBuilder.DropColumn(
                name: "OrgPositionInstance_LocationId",
                table: "ResourceAllocationRequests");

            migrationBuilder.DropColumn(
                name: "OrgPositionInstance_Obs",
                table: "ResourceAllocationRequests");

            migrationBuilder.DropColumn(
                name: "OrgPositionInstance_Workload",
                table: "ResourceAllocationRequests");

            migrationBuilder.RenameColumn(
                name: "OrgPositionInstance_Id",
                table: "ResourceAllocationRequests",
                newName: "OrgPositionInstanceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OrgPositionInstanceId",
                table: "ResourceAllocationRequests",
                newName: "OrgPositionInstance_Id");

            migrationBuilder.AddColumn<DateTime>(
                name: "OrgPositionInstance_AppliesFrom",
                table: "ResourceAllocationRequests",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "OrgPositionInstance_AppliesTo",
                table: "ResourceAllocationRequests",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "OrgPositionInstance_LocationId",
                table: "ResourceAllocationRequests",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "OrgPositionInstance_Obs",
                table: "ResourceAllocationRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "OrgPositionInstance_Workload",
                table: "ResourceAllocationRequests",
                type: "float",
                nullable: true);
        }
    }
}
