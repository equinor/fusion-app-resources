using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fusion.Resources.Database.Migrations
{
    public partial class ResponsibilityMatrix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ResponsibilityMatrices",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Created = table.Column<DateTimeOffset>(nullable: false),
                    CreatedById = table.Column<Guid>(nullable: false),
                    ProjectId = table.Column<Guid>(nullable: false),
                    LocationId = table.Column<Guid>(nullable: false),
                    Discipline = table.Column<string>(nullable: true),
                    BasePositionId = table.Column<Guid>(nullable: false),
                    Sector = table.Column<string>(nullable: true),
                    Unit = table.Column<string>(nullable: true),
                    ResponsibleId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponsibilityMatrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResponsibilityMatrices_Persons_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResponsibilityMatrices_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResponsibilityMatrices_Persons_ResponsibleId",
                        column: x => x.ResponsibleId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityMatrices_CreatedById",
                table: "ResponsibilityMatrices",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityMatrices_ProjectId",
                table: "ResponsibilityMatrices",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityMatrices_ResponsibleId",
                table: "ResponsibilityMatrices",
                column: "ResponsibleId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResponsibilityMatrices");
        }
    }
}
