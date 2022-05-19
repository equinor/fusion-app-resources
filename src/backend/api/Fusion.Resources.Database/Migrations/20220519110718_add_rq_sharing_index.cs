using Microsoft.EntityFrameworkCore.Migrations;

namespace Fusion.Resources.Database.Migrations
{
    public partial class add_rq_sharing_index : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SharedRequests_SharedWithId",
                table: "SharedRequests");

            migrationBuilder.AlterColumn<bool>(
                name: "IsRevoked",
                table: "SharedRequests",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.CreateIndex(
                name: "IX_SharedRequests_SharedWithId_RequestId_IsRevoked",
                table: "SharedRequests",
                columns: new[] { "SharedWithId", "RequestId", "IsRevoked" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SharedRequests_SharedWithId_RequestId_IsRevoked",
                table: "SharedRequests");

            migrationBuilder.AlterColumn<bool>(
                name: "IsRevoked",
                table: "SharedRequests",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_SharedRequests_SharedWithId",
                table: "SharedRequests",
                column: "SharedWithId");
        }
    }
}
