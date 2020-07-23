using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fusion.Resources.Database.Migrations
{
    public partial class Addeddelegatedroles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DelegatedRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PersonId = table.Column<Guid>(nullable: false),
                    Type = table.Column<int>(nullable: false),
                    Classification = table.Column<int>(nullable: false),
                    Created = table.Column<DateTimeOffset>(nullable: false),
                    ValidTo = table.Column<DateTimeOffset>(nullable: false),
                    RecertifiedDate = table.Column<DateTimeOffset>(nullable: true),
                    CreatedById = table.Column<Guid>(nullable: false),
                    RecertifiedById = table.Column<Guid>(nullable: true),
                    ContractId = table.Column<Guid>(nullable: false),
                    ProjectId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DelegatedRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DelegatedRoles_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DelegatedRoles_Persons_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DelegatedRoles_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DelegatedRoles_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DelegatedRoles_Persons_RecertifiedById",
                        column: x => x.RecertifiedById,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DelegatedRoles_ContractId",
                table: "DelegatedRoles",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_DelegatedRoles_CreatedById",
                table: "DelegatedRoles",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DelegatedRoles_PersonId",
                table: "DelegatedRoles",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_DelegatedRoles_ProjectId",
                table: "DelegatedRoles",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_DelegatedRoles_RecertifiedById",
                table: "DelegatedRoles",
                column: "RecertifiedById");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DelegatedRoles");
        }
    }
}
