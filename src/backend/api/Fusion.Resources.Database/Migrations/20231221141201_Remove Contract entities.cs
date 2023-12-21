using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fusion.Resources.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveContractentities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractorRequests");

            migrationBuilder.DropTable(
                name: "ContractPersonnelReplacementChanges");

            migrationBuilder.DropTable(
                name: "DbPersonnelDiscipline");

            migrationBuilder.DropTable(
                name: "DelegatedRoles");

            migrationBuilder.DropTable(
                name: "ContractPersonnel");

            migrationBuilder.DropTable(
                name: "Contracts");

            migrationBuilder.DropTable(
                name: "ExternalPersonnel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContractPersonnelReplacementChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FromPerson = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToPerson = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UPN = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractPersonnelReplacementChanges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllocatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Allocated = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ContractNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrgContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contracts_Persons_AllocatedById",
                        column: x => x.AllocatedById,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Contracts_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExternalPersonnel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AzureUniqueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DawinciCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Deleted = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    JobTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LinkedInProfile = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Mail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PersonIdReplacements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PreferredContractMail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UPN = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalPersonnel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DelegatedRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecertifiedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Classification = table.Column<int>(type: "int", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RecertifiedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    ValidTo = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DelegatedRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DelegatedRoles_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DelegatedRoles_Persons_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DelegatedRoles_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DelegatedRoles_Persons_RecertifiedById",
                        column: x => x.RecertifiedById,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DelegatedRoles_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContractPersonnel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Updated = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractPersonnel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractPersonnel_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractPersonnel_ExternalPersonnel_PersonId",
                        column: x => x.PersonId,
                        principalTable: "ExternalPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContractPersonnel_Persons_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractPersonnel_Persons_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractPersonnel_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DbPersonnelDiscipline",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DbExternalPersonnelPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PersonnelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbPersonnelDiscipline", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DbPersonnelDiscipline_ExternalPersonnel_DbExternalPersonnelPersonId",
                        column: x => x.DbExternalPersonnelPersonId,
                        principalTable: "ExternalPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContractorRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastActivity = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0))),
                    OriginalPositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Updated = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Position_AppliesFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Position_AppliesTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Position_BasePositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position_Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Position_Obs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Position_Workload = table.Column<double>(type: "float", nullable: false),
                    Position_TaskOwner_PositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Position_TaskOwner_RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProvisioningStatus_ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProvisioningStatus_ErrorPayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProvisioningStatus_PositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProvisioningStatus_Provisioned = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ProvisioningStatus_State = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractorRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractorRequests_ContractPersonnel_PersonId",
                        column: x => x.PersonId,
                        principalTable: "ContractPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContractorRequests_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractorRequests_Persons_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractorRequests_Persons_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractorRequests_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContractorRequests_ContractId",
                table: "ContractorRequests",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractorRequests_CreatedById",
                table: "ContractorRequests",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ContractorRequests_LastActivity",
                table: "ContractorRequests",
                column: "LastActivity")
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.CreateIndex(
                name: "IX_ContractorRequests_PersonId",
                table: "ContractorRequests",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractorRequests_ProjectId",
                table: "ContractorRequests",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractorRequests_UpdatedById",
                table: "ContractorRequests",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ContractPersonnel_ContractId",
                table: "ContractPersonnel",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractPersonnel_CreatedById",
                table: "ContractPersonnel",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ContractPersonnel_PersonId",
                table: "ContractPersonnel",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractPersonnel_ProjectId",
                table: "ContractPersonnel",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractPersonnel_UpdatedById",
                table: "ContractPersonnel",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ContractPersonnelReplacementChanges_ProjectId_ContractId",
                table: "ContractPersonnelReplacementChanges",
                columns: new[] { "ProjectId", "ContractId" })
                .Annotation("SqlServer:Clustered", false)
                .Annotation("SqlServer:Include", new[] { "UPN", "FromPerson", "ToPerson", "ChangeType", "Created", "CreatedBy" });

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_AllocatedById",
                table: "Contracts",
                column: "AllocatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_ProjectId",
                table: "Contracts",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_DbPersonnelDiscipline_DbExternalPersonnelPersonId",
                table: "DbPersonnelDiscipline",
                column: "DbExternalPersonnelPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_DelegatedRoles_ContractId",
                table: "DelegatedRoles",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_DelegatedRoles_CreatedById",
                table: "DelegatedRoles",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DelegatedRoles_PersonId",
                table: "DelegatedRoles",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_DelegatedRoles_ProjectId",
                table: "DelegatedRoles",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_DelegatedRoles_RecertifiedById",
                table: "DelegatedRoles",
                column: "RecertifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalPersonnel_Mail",
                table: "ExternalPersonnel",
                column: "Mail")
                .Annotation("SqlServer:Clustered", false);
        }
    }
}
