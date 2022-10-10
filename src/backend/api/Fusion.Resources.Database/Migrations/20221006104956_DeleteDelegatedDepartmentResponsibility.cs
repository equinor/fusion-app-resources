using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fusion.Resources.Database.Migrations
{
    public partial class DeleteDelegatedDepartmentResponsibility : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DepartmentResponsibles");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DepartmentResponsibles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateCreated = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "getutcdate()"),
                    DateFrom = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DateTo = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DateUpdated = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, defaultValueSql: "getutcdate()"),
                    DepartmentId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponsibleAzureObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentResponsibles", x => x.Id);
                });
        }
    }
}
