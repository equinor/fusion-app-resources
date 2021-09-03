using Microsoft.EntityFrameworkCore.Migrations;

namespace Fusion.Resources.Database.Migrations
{
    public partial class rename_request_tasks_actions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequestTasks_Persons_ResolvedById",
                table: "RequestTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestTasks_Persons_SentById",
                table: "RequestTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestTasks_ResourceAllocationRequests_RequestId",
                table: "RequestTasks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RequestTasks",
                table: "RequestTasks");

            migrationBuilder.RenameTable(
                name: "RequestTasks",
                newName: "RequestActions");

            migrationBuilder.RenameIndex(
                name: "IX_RequestTasks_SentById",
                table: "RequestActions",
                newName: "IX_RequestActions_SentById");

            migrationBuilder.RenameIndex(
                name: "IX_RequestTasks_ResolvedById",
                table: "RequestActions",
                newName: "IX_RequestActions_ResolvedById");

            migrationBuilder.RenameIndex(
                name: "IX_RequestTasks_RequestId",
                table: "RequestActions",
                newName: "IX_RequestActions_RequestId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RequestActions",
                table: "RequestActions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RequestActions_Persons_ResolvedById",
                table: "RequestActions",
                column: "ResolvedById",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestActions_Persons_SentById",
                table: "RequestActions",
                column: "SentById",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestActions_ResourceAllocationRequests_RequestId",
                table: "RequestActions",
                column: "RequestId",
                principalTable: "ResourceAllocationRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequestActions_Persons_ResolvedById",
                table: "RequestActions");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestActions_Persons_SentById",
                table: "RequestActions");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestActions_ResourceAllocationRequests_RequestId",
                table: "RequestActions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RequestActions",
                table: "RequestActions");

            migrationBuilder.RenameTable(
                name: "RequestActions",
                newName: "RequestTasks");

            migrationBuilder.RenameIndex(
                name: "IX_RequestActions_SentById",
                table: "RequestTasks",
                newName: "IX_RequestTasks_SentById");

            migrationBuilder.RenameIndex(
                name: "IX_RequestActions_ResolvedById",
                table: "RequestTasks",
                newName: "IX_RequestTasks_ResolvedById");

            migrationBuilder.RenameIndex(
                name: "IX_RequestActions_RequestId",
                table: "RequestTasks",
                newName: "IX_RequestTasks_RequestId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RequestTasks",
                table: "RequestTasks",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RequestTasks_Persons_ResolvedById",
                table: "RequestTasks",
                column: "ResolvedById",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestTasks_Persons_SentById",
                table: "RequestTasks",
                column: "SentById",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestTasks_ResourceAllocationRequests_RequestId",
                table: "RequestTasks",
                column: "RequestId",
                principalTable: "ResourceAllocationRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
