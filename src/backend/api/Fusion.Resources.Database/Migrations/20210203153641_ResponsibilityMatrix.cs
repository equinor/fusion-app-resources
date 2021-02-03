using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fusion.Resources.Database.migrations
{
    public partial class ResponsibilityMatrix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastActivity",
                table: "ResourceAllocationRequests",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(2021, 2, 3, 15, 36, 41, 259, DateTimeKind.Unspecified).AddTicks(8146), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldDefaultValue: new DateTimeOffset(new DateTime(2021, 1, 29, 15, 51, 10, 119, DateTimeKind.Unspecified).AddTicks(3155), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Created",
                table: "ResourceAllocationRequests",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(2021, 2, 3, 15, 36, 41, 252, DateTimeKind.Unspecified).AddTicks(3819), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldDefaultValue: new DateTimeOffset(new DateTime(2021, 1, 29, 15, 51, 10, 112, DateTimeKind.Unspecified).AddTicks(4321), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateTable(
                name: "ResponsibilityMatrices",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ProjectId = table.Column<Guid>(nullable: true),
                    LocationId = table.Column<Guid>(nullable: true),
                    Discipline = table.Column<string>(nullable: true),
                    BasePositionId = table.Column<Guid>(nullable: true),
                    Sector = table.Column<string>(nullable: true),
                    Unit = table.Column<string>(nullable: true),
                    ResponsibleId = table.Column<Guid>(nullable: true),
                    Created = table.Column<DateTimeOffset>(nullable: false),
                    CreatedById = table.Column<Guid>(nullable: false),
                    Updated = table.Column<DateTimeOffset>(nullable: true),
                    UpdatedById = table.Column<Guid>(nullable: true)
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
                    table.ForeignKey(
                        name: "FK_ResponsibilityMatrices_Persons_UpdatedById",
                        column: x => x.UpdatedById,
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

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityMatrices_UpdatedById",
                table: "ResponsibilityMatrices",
                column: "UpdatedById");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResponsibilityMatrices");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastActivity",
                table: "ResourceAllocationRequests",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(2021, 1, 29, 15, 51, 10, 119, DateTimeKind.Unspecified).AddTicks(3155), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldDefaultValue: new DateTimeOffset(new DateTime(2021, 2, 3, 15, 36, 41, 259, DateTimeKind.Unspecified).AddTicks(8146), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Created",
                table: "ResourceAllocationRequests",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(2021, 1, 29, 15, 51, 10, 112, DateTimeKind.Unspecified).AddTicks(4321), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldDefaultValue: new DateTimeOffset(new DateTime(2021, 2, 3, 15, 36, 41, 252, DateTimeKind.Unspecified).AddTicks(3819), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
