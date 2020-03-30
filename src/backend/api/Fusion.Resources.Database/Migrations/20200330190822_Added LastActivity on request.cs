using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fusion.Resources.Database.Migrations
{
    public partial class AddedLastActivityonrequest : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastActivity",
                table: "ContractorRequests",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastActivity",
                table: "ContractorRequests");
        }
    }
}
