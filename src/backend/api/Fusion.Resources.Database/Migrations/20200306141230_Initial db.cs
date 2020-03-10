using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fusion.Resources.Database.Migrations
{
    public partial class Initialdb : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExternalPersonnel",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    AzureUniqueId = table.Column<Guid>(nullable: true),
                    AccountStatus = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    FirstName = table.Column<string>(nullable: true),
                    LastName = table.Column<string>(nullable: true),
                    Mail = table.Column<string>(nullable: true),
                    Phone = table.Column<string>(nullable: true),
                    JobTitle = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalPersonnel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Persons",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    AzureUniqueId = table.Column<Guid>(nullable: false),
                    Mail = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    Phone = table.Column<string>(nullable: true),
                    AccountType = table.Column<string>(nullable: true),
                    JobTitle = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Persons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    DomainId = table.Column<string>(nullable: true),
                    OrgProjectId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DbPersonnelDiscipline",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PersonnelId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    DbExternalPersonnelPersonId = table.Column<Guid>(nullable: true)
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
                name: "Contracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ContractNumber = table.Column<string>(nullable: true),
                    OrgContractId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    ProjectId = table.Column<Guid>(nullable: false),
                    Allocated = table.Column<DateTimeOffset>(nullable: false),
                    AllocatedById = table.Column<Guid>(nullable: false)
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
                name: "ContractPersonnel",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ContractId = table.Column<Guid>(nullable: false),
                    ProjectId = table.Column<Guid>(nullable: false),
                    PersonId = table.Column<Guid>(nullable: false),
                    Created = table.Column<DateTimeOffset>(nullable: false),
                    Updated = table.Column<DateTimeOffset>(nullable: true),
                    CreatedById = table.Column<Guid>(nullable: false),
                    UpdatedById = table.Column<Guid>(nullable: true)
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
                        name: "FK_ContractPersonnel_Persons_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractPersonnel_ExternalPersonnel_PersonId",
                        column: x => x.PersonId,
                        principalTable: "ExternalPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContractPersonnel_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractPersonnel_Persons_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContractorRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    ContractId = table.Column<Guid>(nullable: false),
                    ProjectId = table.Column<Guid>(nullable: false),
                    PersonId = table.Column<Guid>(nullable: false),
                    Position_Name = table.Column<string>(nullable: true),
                    Position_BasePositionId = table.Column<Guid>(nullable: true),
                    Position_AppliesFrom = table.Column<DateTime>(nullable: true),
                    Position_AppliesTo = table.Column<DateTime>(nullable: true),
                    Position_Obs = table.Column<string>(nullable: true),
                    Position_Workload = table.Column<double>(nullable: true),
                    Position_TaskOwner_PositionId = table.Column<Guid>(nullable: true),
                    Position_TaskOwner_RequestId = table.Column<Guid>(nullable: true),
                    State = table.Column<string>(nullable: false),
                    ProvisioningStatus_State = table.Column<string>(nullable: true),
                    ProvisioningStatus_PositionId = table.Column<Guid>(nullable: true),
                    ProvisioningStatus_Provisioned = table.Column<DateTimeOffset>(nullable: true),
                    ProvisioningStatus_ErrorMessage = table.Column<string>(nullable: true),
                    ProvisioningStatus_ErrorPayload = table.Column<string>(nullable: true),
                    Created = table.Column<DateTimeOffset>(nullable: false),
                    Updated = table.Column<DateTimeOffset>(nullable: true),
                    CreatedById = table.Column<Guid>(nullable: false),
                    UpdatedById = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractorRequests", x => x.Id);
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
                        name: "FK_ContractorRequests_ContractPersonnel_PersonId",
                        column: x => x.PersonId,
                        principalTable: "ContractPersonnel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContractorRequests_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractorRequests_Persons_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Persons",
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
                name: "IX_ExternalPersonnel_Mail",
                table: "ExternalPersonnel",
                column: "Mail")
                .Annotation("SqlServer:Clustered", false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractorRequests");

            migrationBuilder.DropTable(
                name: "DbPersonnelDiscipline");

            migrationBuilder.DropTable(
                name: "ContractPersonnel");

            migrationBuilder.DropTable(
                name: "Contracts");

            migrationBuilder.DropTable(
                name: "ExternalPersonnel");

            migrationBuilder.DropTable(
                name: "Persons");

            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
