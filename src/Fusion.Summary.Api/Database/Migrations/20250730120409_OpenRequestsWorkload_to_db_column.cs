using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fusion.Summary.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class OpenRequestsWorkload_to_db_column : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OpenRequestsWorkload",
                table: "WeeklySummaryReports",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OpenRequestsWorkload",
                table: "WeeklySummaryReports");
        }
    }
}
