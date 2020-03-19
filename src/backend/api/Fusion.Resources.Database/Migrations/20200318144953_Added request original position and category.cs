using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fusion.Resources.Database.Migrations
{
    public partial class Addedrequestoriginalpositionandcategory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "ContractorRequests",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "OriginalPositionId",
                table: "ContractorRequests",
                nullable: true);

            migrationBuilder.UpdateData("ContractorRequests", "Category", "", "Category", "NewRequest");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "ContractorRequests");

            migrationBuilder.DropColumn(
                name: "OriginalPositionId",
                table: "ContractorRequests");
        }
    }
}
