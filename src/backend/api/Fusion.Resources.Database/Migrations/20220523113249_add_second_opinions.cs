using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fusion.Resources.Database.Migrations
{
    public partial class add_second_opinions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SecondOpinions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecondOpinions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DbSecondOpinionResponse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PromptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedToId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnsweredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    State = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbSecondOpinionResponse", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DbSecondOpinionResponse_Persons_AssignedToId",
                        column: x => x.AssignedToId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DbSecondOpinionResponse_SecondOpinions_PromptId",
                        column: x => x.PromptId,
                        principalTable: "SecondOpinions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DbSecondOpinionResponse_AssignedToId",
                table: "DbSecondOpinionResponse",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_DbSecondOpinionResponse_PromptId",
                table: "DbSecondOpinionResponse",
                column: "PromptId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DbSecondOpinionResponse");

            migrationBuilder.DropTable(
                name: "SecondOpinions");
        }
    }
}
