using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fusion.Resources.Database.Migrations
{
    public partial class Addedchangerequestsupport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApplicableChangeDate",
                table: "ResourceAllocationRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubType",
                table: "ResourceAllocationRequests",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicableChangeDate",
                table: "ResourceAllocationRequests");

            migrationBuilder.DropColumn(
                name: "SubType",
                table: "ResourceAllocationRequests");
        }
    }
}
