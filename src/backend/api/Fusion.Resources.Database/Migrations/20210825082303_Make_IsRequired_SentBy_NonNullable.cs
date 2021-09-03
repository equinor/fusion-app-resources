using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fusion.Resources.Database.Migrations
{
    public partial class Make_IsRequired_SentBy_NonNullable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequestTasks_Persons_SentById",
                table: "RequestTasks");

            migrationBuilder.AlterColumn<Guid>(
                name: "SentById",
                table: "RequestTasks",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestTasks_Persons_SentById",
                table: "RequestTasks",
                column: "SentById",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequestTasks_Persons_SentById",
                table: "RequestTasks");

            migrationBuilder.AlterColumn<Guid>(
                name: "SentById",
                table: "RequestTasks",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_RequestTasks_Persons_SentById",
                table: "RequestTasks",
                column: "SentById",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
