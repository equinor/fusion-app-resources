using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fusion.Resources.Database.Migrations
{
    public partial class Add_IsRequired_SentBy_ToRequestActions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRequired",
                table: "RequestTasks",
                type: "bit",
                nullable: false,
                defaultValueSql: "(0)");

            migrationBuilder.AddColumn<Guid>(
                name: "SentById",
                table: "RequestTasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestTasks_SentById",
                table: "RequestTasks",
                column: "SentById");

            migrationBuilder.AddForeignKey(
                name: "FK_RequestTasks_Persons_SentById",
                table: "RequestTasks",
                column: "SentById",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Set sent by = 'Fusion Resources Admin' 
            migrationBuilder.Sql("update dbo.RequestTasks set SentById = (" +
                "select top 1 Id " +
                "from Persons " +
                "where AzureUniqueId = '9561207e-f642-47ee-98f4-b9e864bf1110') " +
            "where SentById IS NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequestTasks_Persons_SentById",
                table: "RequestTasks");

            migrationBuilder.DropIndex(
                name: "IX_RequestTasks_SentById",
                table: "RequestTasks");

            migrationBuilder.DropColumn(
                name: "IsRequired",
                table: "RequestTasks");

            migrationBuilder.DropColumn(
                name: "SentById",
                table: "RequestTasks");
        }
    }
}
