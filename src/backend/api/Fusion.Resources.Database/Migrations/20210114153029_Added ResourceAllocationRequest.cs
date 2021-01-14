using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fusion.Resources.Database.Migrations
{
    public partial class AddedResourceAllocationRequest : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ResourceAllocationRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Discipline = table.Column<string>(nullable: true),
                    Type = table.Column<string>(nullable: false),
                    State = table.Column<string>(nullable: false),
                    ProjectId = table.Column<Guid>(nullable: false),
                    OriginalPositionId = table.Column<Guid>(nullable: true),
                    ResourceAllocationOrgPositionInstance_Id = table.Column<Guid>(nullable: true),
                    ResourceAllocationOrgPositionInstance_Workload = table.Column<double>(nullable: true),
                    ResourceAllocationOrgPositionInstance_Obs = table.Column<string>(nullable: true),
                    ResourceAllocationOrgPositionInstance_AppliesFrom = table.Column<DateTime>(nullable: true),
                    ResourceAllocationOrgPositionInstance_AppliesTo = table.Column<DateTime>(nullable: true),
                    ResourceAllocationOrgPositionInstance_Location = table.Column<string>(nullable: true),
                    AdditionalNote = table.Column<string>(nullable: true),
                    ProposedChanges = table.Column<string>(nullable: true),
                    ProposedPersonId = table.Column<Guid>(nullable: false),
                    Created = table.Column<DateTimeOffset>(nullable: false, defaultValue: new DateTimeOffset(new DateTime(2021, 1, 14, 15, 30, 28, 655, DateTimeKind.Unspecified).AddTicks(2798), new TimeSpan(0, 0, 0, 0, 0))),
                    Updated = table.Column<DateTimeOffset>(nullable: true),
                    CreatedById = table.Column<Guid>(nullable: false),
                    UpdatedById = table.Column<Guid>(nullable: true),
                    LastActivity = table.Column<DateTimeOffset>(nullable: false, defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0))),
                    IsDraft = table.Column<bool>(nullable: false),
                    ProvisioningStatus_State = table.Column<string>(nullable: true),
                    ProvisioningStatus_PositionId = table.Column<Guid>(nullable: true),
                    ProvisioningStatus_Provisioned = table.Column<DateTimeOffset>(nullable: true),
                    ProvisioningStatus_ErrorMessage = table.Column<string>(nullable: true),
                    ProvisioningStatus_ErrorPayload = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceAllocationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceAllocationRequests_Persons_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResourceAllocationRequests_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResourceAllocationRequests_Persons_ProposedPersonId",
                        column: x => x.ProposedPersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResourceAllocationRequests_Persons_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceAllocationRequests_CreatedById",
                table: "ResourceAllocationRequests",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceAllocationRequests_LastActivity",
                table: "ResourceAllocationRequests",
                column: "LastActivity")
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceAllocationRequests_ProjectId",
                table: "ResourceAllocationRequests",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceAllocationRequests_ProposedPersonId",
                table: "ResourceAllocationRequests",
                column: "ProposedPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceAllocationRequests_UpdatedById",
                table: "ResourceAllocationRequests",
                column: "UpdatedById");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResourceAllocationRequests");
        }
    }
}
