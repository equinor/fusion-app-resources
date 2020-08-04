using Microsoft.EntityFrameworkCore.Migrations;

namespace Fusion.Resources.Database.Migrations
{
    public partial class Addeduniqueonpersonazureid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Mail",
                table: "Persons",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Persons_AzureUniqueId",
                table: "Persons",
                column: "AzureUniqueId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Persons_Mail",
                table: "Persons",
                column: "Mail")
                .Annotation("SqlServer:Clustered", false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Persons_AzureUniqueId",
                table: "Persons");

            migrationBuilder.DropIndex(
                name: "IX_Persons_Mail",
                table: "Persons");

            migrationBuilder.AlterColumn<string>(
                name: "Mail",
                table: "Persons",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string));
        }
    }
}
