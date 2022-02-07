using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fusion.Resources.Database.Migrations
{
    public partial class ContractPersonnelReplacementAuditing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PersonIdReplacements",
                table: "ExternalPersonnel",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ContractPersonnelReplacementChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UPN = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FromPerson = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ToPerson = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractPersonnelReplacementChanges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContractPersonnelReplacementChanges_ProjectId_ContractId",
                table: "ContractPersonnelReplacementChanges",
                columns: new[] { "ProjectId", "ContractId" })
                .Annotation("SqlServer:Clustered", false)
                .Annotation("SqlServer:Include", new[] { "UPN", "FromPerson", "ToPerson", "ChangeType", "Created", "CreatedBy" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractPersonnelReplacementChanges");

            migrationBuilder.DropColumn(
                name: "PersonIdReplacements",
                table: "ExternalPersonnel");
        }
    }
}
