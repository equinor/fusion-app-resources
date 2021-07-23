using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fusion.Resources.Database.Migrations
{
    public partial class add_request_conversation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RequestConversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    SenderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Recpient = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertiesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sent = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestConversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestConversations_Persons_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestConversations_ResourceAllocationRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "ResourceAllocationRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequestConversations_RequestId",
                table: "RequestConversations",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestConversations_SenderId",
                table: "RequestConversations",
                column: "SenderId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestConversations");
        }
    }
}
