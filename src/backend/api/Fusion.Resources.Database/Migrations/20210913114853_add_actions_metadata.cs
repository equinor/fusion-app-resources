using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fusion.Resources.Database.Migrations
{
    public partial class add_actions_metadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Body",
                table: "RequestActions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedToId",
                table: "RequestActions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "RequestActions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestActions_AssignedToId",
                table: "RequestActions",
                column: "AssignedToId");

            migrationBuilder.AddForeignKey(
                name: "FK_RequestActions_Persons_AssignedToId",
                table: "RequestActions",
                column: "AssignedToId",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequestActions_Persons_AssignedToId",
                table: "RequestActions");

            migrationBuilder.DropIndex(
                name: "IX_RequestActions_AssignedToId",
                table: "RequestActions");

            migrationBuilder.DropColumn(
                name: "AssignedToId",
                table: "RequestActions");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "RequestActions");

            migrationBuilder.AlterColumn<string>(
                name: "Body",
                table: "RequestActions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);
        }
    }
}
