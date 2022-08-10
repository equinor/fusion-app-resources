using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fusion.Resources.Database.Migrations
{
    public partial class add_candidates_prop_for_requests : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DbPersonDbResourceAllocationRequest",
                columns: table => new
                {
                    CandidateForId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidatesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbPersonDbResourceAllocationRequest", x => new { x.CandidateForId, x.CandidatesId });
                    table.ForeignKey(
                        name: "FK_DbPersonDbResourceAllocationRequest_Persons_CandidatesId",
                        column: x => x.CandidatesId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DbPersonDbResourceAllocationRequest_ResourceAllocationRequests_CandidateForId",
                        column: x => x.CandidateForId,
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
