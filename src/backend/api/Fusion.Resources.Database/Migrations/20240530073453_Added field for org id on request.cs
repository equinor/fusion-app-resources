using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fusion.Resources.Database.Migrations
{
    /// <inheritdoc />
    public partial class Addedfieldfororgidonrequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedDepartmentId",
                table: "ResourceAllocationRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceAllocationRequests_AssignedDepartment",
                table: "ResourceAllocationRequests",
                column: "AssignedDepartment")
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceAllocationRequests_AssignedDepartmentId",
                table: "ResourceAllocationRequests",
                column: "AssignedDepartmentId")
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceAllocationRequests_RequestNumber",
                table: "ResourceAllocationRequests",
                column: "RequestNumber")
                .Annotation("SqlServer:Clustered", false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ResourceAllocationRequests_AssignedDepartment",
                table: "ResourceAllocationRequests");

            migrationBuilder.DropIndex(
                name: "IX_ResourceAllocationRequests_AssignedDepartmentId",
                table: "ResourceAllocationRequests");

            migrationBuilder.DropIndex(
                name: "IX_ResourceAllocationRequests_RequestNumber",
                table: "ResourceAllocationRequests");

            migrationBuilder.DropColumn(
                name: "AssignedDepartmentId",
                table: "ResourceAllocationRequests");
        }
    }
}
