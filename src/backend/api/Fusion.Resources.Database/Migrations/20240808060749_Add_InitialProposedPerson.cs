using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fusion.Resources.Database.Migrations
{
    /// <inheritdoc />
    public partial class Add_InitialProposedPerson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "InitialProposedPerson_AzureUniqueId",
                table: "ResourceAllocationRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InitialProposedPerson_Mail",
                table: "ResourceAllocationRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InitialProposedPerson_AzureUniqueId",
                table: "ResourceAllocationRequests");

            migrationBuilder.DropColumn(
                name: "InitialProposedPerson_Mail",
                table: "ResourceAllocationRequests");
        }
    }
}
