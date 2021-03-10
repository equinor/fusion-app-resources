using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fusion.Resources.Database.Migrations
{
    public partial class Refactoredinternalrequests : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ResourceAllocationRequests_Persons_ProposedPersonId",
                table: "ResourceAllocationRequests");

            migrationBuilder.DropIndex(
                name: "IX_ResourceAllocationRequests_ProposedPersonId",
                table: "ResourceAllocationRequests");

            migrationBuilder.DropColumn(
                name: "ProposedPersonWasNotified",
                table: "ResourceAllocationRequests");

            migrationBuilder.RenameColumn(
                name: "State",
                table: "ResourceAllocationRequests",
                newName: "State_State");

            migrationBuilder.RenameColumn(
                name: "ProvisioningStatus_PositionId",
                table: "ResourceAllocationRequests",
                newName: "ProvisioningStatus_OrgProjectId");

            migrationBuilder.RenameColumn(
                name: "ProposedPersonId",
                table: "ResourceAllocationRequests",
                newName: "ProvisioningStatus_OrgPositionId");

            migrationBuilder.AddColumn<string>(
                name: "WorkflowClassType",
                table: "Workflows",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "State_State",
                table: "ResourceAllocationRequests",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "OrgPositionInstance_AssignedToMail",
                table: "ResourceAllocationRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrgPositionInstance_AssignedToUniqueId",
                table: "ResourceAllocationRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProposedPerson_AzureUniqueId",
                table: "ResourceAllocationRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ProposedPerson_HasBeenProposed",
                table: "ResourceAllocationRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProposedPerson_Mail",
                table: "ResourceAllocationRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProposedPerson_ProposedAt",
                table: "ResourceAllocationRequests",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ProposedPerson_WasNotified",
                table: "ResourceAllocationRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ProvisioningStatus_OrgInstanceId",
                table: "ResourceAllocationRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestOwner",
                table: "ResourceAllocationRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "State_IsCompleted",
                table: "ResourceAllocationRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkflowClassType",
                table: "Workflows");

            migrationBuilder.DropColumn(
                name: "OrgPositionInstance_AssignedToMail",
                table: "ResourceAllocationRequests");

            migrationBuilder.DropColumn(
                name: "OrgPositionInstance_AssignedToUniqueId",
                table: "ResourceAllocationRequests");

            migrationBuilder.DropColumn(
                name: "ProposedPerson_AzureUniqueId",
                table: "ResourceAllocationRequests");

            migrationBuilder.DropColumn(
                name: "ProposedPerson_HasBeenProposed",
                table: "ResourceAllocationRequests");

            migrationBuilder.DropColumn(
                name: "ProposedPerson_Mail",
                table: "ResourceAllocationRequests");

            migrationBuilder.DropColumn(
                name: "ProposedPerson_ProposedAt",
                table: "ResourceAllocationRequests");

            migrationBuilder.DropColumn(
                name: "ProposedPerson_WasNotified",
                table: "ResourceAllocationRequests");

            migrationBuilder.DropColumn(
                name: "ProvisioningStatus_OrgInstanceId",
                table: "ResourceAllocationRequests");

            migrationBuilder.DropColumn(
                name: "RequestOwner",
                table: "ResourceAllocationRequests");

            migrationBuilder.DropColumn(
                name: "State_IsCompleted",
                table: "ResourceAllocationRequests");

            migrationBuilder.RenameColumn(
                name: "State_State",
                table: "ResourceAllocationRequests",
                newName: "State");

            migrationBuilder.RenameColumn(
                name: "ProvisioningStatus_OrgProjectId",
                table: "ResourceAllocationRequests",
                newName: "ProvisioningStatus_PositionId");

            migrationBuilder.RenameColumn(
                name: "ProvisioningStatus_OrgPositionId",
                table: "ResourceAllocationRequests",
                newName: "ProposedPersonId");

            migrationBuilder.AlterColumn<string>(
                name: "State",
                table: "ResourceAllocationRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ProposedPersonWasNotified",
                table: "ResourceAllocationRequests",
                type: "bit",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceAllocationRequests_ProposedPersonId",
                table: "ResourceAllocationRequests",
                column: "ProposedPersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_ResourceAllocationRequests_Persons_ProposedPersonId",
                table: "ResourceAllocationRequests",
                column: "ProposedPersonId",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
