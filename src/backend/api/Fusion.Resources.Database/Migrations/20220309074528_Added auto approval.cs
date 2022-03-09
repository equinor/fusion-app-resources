using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fusion.Resources.Database.Migrations
{
    public partial class Addedautoapproval : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DepartmentAutoApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentFullPath = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IncludeSubDepartments = table.Column<bool>(type: "bit", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentAutoApprovals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentAutoApprovals_DepartmentFullPath",
                table: "DepartmentAutoApprovals",
                column: "DepartmentFullPath")
                .Annotation("SqlServer:Clustered", false)
                .Annotation("SqlServer:Include", new[] { "IncludeSubDepartments", "Enabled" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DepartmentAutoApprovals");
        }
    }
}
