using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fusion.Resources.Database.Migrations
{
    public partial class ResponsibilityMatrix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "ResponsibleId",
                table: "ResponsibilityMatrices",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectId",
                table: "ResponsibilityMatrices",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "LocationId",
                table: "ResponsibilityMatrices",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "BasePositionId",
                table: "ResponsibilityMatrices",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Updated",
                table: "ResponsibilityMatrices",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedById",
                table: "ResponsibilityMatrices",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityMatrices_UpdatedById",
                table: "ResponsibilityMatrices",
                column: "UpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ResponsibilityMatrices_Persons_UpdatedById",
                table: "ResponsibilityMatrices",
                column: "UpdatedById",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ResponsibilityMatrices_Persons_UpdatedById",
                table: "ResponsibilityMatrices");

            migrationBuilder.DropIndex(
                name: "IX_ResponsibilityMatrices_UpdatedById",
                table: "ResponsibilityMatrices");

            migrationBuilder.DropColumn(
                name: "Updated",
                table: "ResponsibilityMatrices");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "ResponsibilityMatrices");

            migrationBuilder.AlterColumn<Guid>(
                name: "ResponsibleId",
                table: "ResponsibilityMatrices",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectId",
                table: "ResponsibilityMatrices",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "LocationId",
                table: "ResponsibilityMatrices",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "BasePositionId",
                table: "ResponsibilityMatrices",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);
        }
    }
}
