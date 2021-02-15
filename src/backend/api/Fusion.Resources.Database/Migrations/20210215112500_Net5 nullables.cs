using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fusion.Resources.Database.Migrations
{
    public partial class Net5nullables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ResourceAllocationRequests_LastActivity",
                table: "ResourceAllocationRequests");

            migrationBuilder.RenameColumn(
                name: "OriginalPositionId",
                table: "ResourceAllocationRequests",
                newName: "OrgPositionId");

            migrationBuilder.AlterColumn<string>(
                name: "ProvisioningStatus_State",
                table: "ResourceAllocationRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "OrgPositionInstance_Id",
                table: "ResourceAllocationRequests",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "OrgPositionInstance_AppliesTo",
                table: "ResourceAllocationRequests",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "OrgPositionInstance_AppliesFrom",
                table: "ResourceAllocationRequests",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastActivity",
                table: "ResourceAllocationRequests",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldDefaultValue: new DateTimeOffset(new DateTime(2021, 2, 3, 15, 36, 41, 259, DateTimeKind.Unspecified).AddTicks(8146), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Created",
                table: "ResourceAllocationRequests",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldDefaultValue: new DateTimeOffset(new DateTime(2021, 2, 3, 15, 36, 41, 252, DateTimeKind.Unspecified).AddTicks(3819), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Persons",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Mail",
                table: "Persons",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "DbWorkflowStep",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ProvisioningStatus_State",
                table: "ContractorRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Position_Workload",
                table: "ContractorRequests",
                type: "float",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Position_BasePositionId",
                table: "ContractorRequests",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Position_AppliesTo",
                table: "ContractorRequests",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Position_AppliesFrom",
                table: "ContractorRequests",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ContractorRequests",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OrgPositionId",
                table: "ResourceAllocationRequests",
                newName: "OriginalPositionId");

            migrationBuilder.AlterColumn<string>(
                name: "ProvisioningStatus_State",
                table: "ResourceAllocationRequests",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrgPositionInstance_Id",
                table: "ResourceAllocationRequests",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<DateTime>(
                name: "OrgPositionInstance_AppliesTo",
                table: "ResourceAllocationRequests",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "OrgPositionInstance_AppliesFrom",
                table: "ResourceAllocationRequests",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastActivity",
                table: "ResourceAllocationRequests",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(2021, 2, 3, 15, 36, 41, 259, DateTimeKind.Unspecified).AddTicks(8146), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Created",
                table: "ResourceAllocationRequests",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(2021, 2, 3, 15, 36, 41, 252, DateTimeKind.Unspecified).AddTicks(3819), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Persons",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Mail",
                table: "Persons",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "DbWorkflowStep",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProvisioningStatus_State",
                table: "ContractorRequests",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<double>(
                name: "Position_Workload",
                table: "ContractorRequests",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<Guid>(
                name: "Position_BasePositionId",
                table: "ContractorRequests",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Position_AppliesTo",
                table: "ContractorRequests",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Position_AppliesFrom",
                table: "ContractorRequests",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ContractorRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceAllocationRequests_LastActivity",
                table: "ResourceAllocationRequests",
                column: "LastActivity")
                .Annotation("SqlServer:Clustered", false);
        }
    }
}
