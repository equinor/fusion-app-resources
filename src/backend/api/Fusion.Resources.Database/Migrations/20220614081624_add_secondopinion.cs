using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fusion.Resources.Database.Migrations
{
    public partial class add_secondopinion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SecondOpinions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecondOpinions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecondOpinions_Persons_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SecondOpinions_ResourceAllocationRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "ResourceAllocationRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SecondOpinionResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PromptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedToId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AnsweredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    State = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecondOpinionResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecondOpinionResponses_Persons_AssignedToId",
                        column: x => x.AssignedToId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SecondOpinionResponses_SecondOpinions_PromptId",
                        column: x => x.PromptId,
                        principalTable: "SecondOpinions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SecondOpinionResponses_AssignedToId",
                table: "SecondOpinionResponses",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_SecondOpinionResponses_PromptId",
                table: "SecondOpinionResponses",
                column: "PromptId");

            migrationBuilder.CreateIndex(
                name: "IX_SecondOpinions_CreatedById",
                table: "SecondOpinions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SecondOpinions_RequestId",
                table: "SecondOpinions",
                column: "RequestId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SecondOpinionResponses");

            migrationBuilder.DropTable(
                name: "SecondOpinions");
        }
    }
}
