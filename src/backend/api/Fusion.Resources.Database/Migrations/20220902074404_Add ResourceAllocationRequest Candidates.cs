using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fusion.Resources.Database.migrations
{
    public partial class AddResourceAllocationRequestCandidates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DbPersonDbResourceAllocationRequest",
                columns: table => new
                {
                    CandidatesForRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidatesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbPersonDbResourceAllocationRequest", x => new { x.CandidatesForRequestId, x.CandidatesId });
                    table.ForeignKey(
                        name: "FK_DbPersonDbResourceAllocationRequest_Persons_CandidatesId",
                        column: x => x.CandidatesId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DbPersonDbResourceAllocationRequest_ResourceAllocationRequests_CandidatesForRequestId",
                        column: x => x.CandidatesForRequestId,
                        principalTable: "ResourceAllocationRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DbPersonDbResourceAllocationRequest_CandidatesId",
                table: "DbPersonDbResourceAllocationRequest",
                column: "CandidatesId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DbPersonDbResourceAllocationRequest");
        }
    }
}
