using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fusion.Resources.Database.Migrations
{
    public partial class add_shared_requests : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SharedRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SharedWithId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SharedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    GrantedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharedRequests_Persons_SharedById",
                        column: x => x.SharedById,
                        principalTable: "Persons",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SharedRequests_Persons_SharedWithId",
                        column: x => x.SharedWithId,
                        principalTable: "Persons",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SharedRequests_ResourceAllocationRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "ResourceAllocationRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SharedRequests_RequestId",
                table: "SharedRequests",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedRequests_SharedById",
                table: "SharedRequests",
                column: "SharedById");

            migrationBuilder.CreateIndex(
                name: "IX_SharedRequests_SharedWithId",
                table: "SharedRequests",
                column: "SharedWithId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SharedRequests");
        }
    }
}
