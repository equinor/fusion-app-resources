using Fusion.ApiClients.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class AllocateContract : TrackableRequest<QueryContract>
    {
        public AllocateContract(Guid orgChartId, string contractNumber)
        {
            OrgChartId = orgChartId;
            ContractNumber = contractNumber;
        }
        public Guid OrgChartId { get; set; }
        public string ContractNumber { get; set; }



        public class Handler : IRequestHandler<AllocateContract, QueryContract>
        {
            private readonly IOrgApiClient orgClient;
            private readonly ResourcesDbContext resourcesDb;

            private ApiClients.Org.ApiProjectV2 project;

            public Handler(IOrgApiClientFactory orgApiClientFactory, ResourcesDbContext resourcesDb)
            {
                orgClient = orgApiClientFactory.CreateClient(ApiClientMode.Application);
                this.resourcesDb = resourcesDb;
            }

            public async Task<QueryContract> Handle(AllocateContract request, CancellationToken cancellationToken)
            {
                project = await orgClient.GetProjectOrDefaultV2Async(request.OrgChartId);

                if (project is null)
                    throw new CommandValidationError($"Could not locate any org chart project with id '{request.OrgChartId}'");


                var contracts = await orgClient.GetContractsV2Async(request.OrgChartId);

                var orgChartContract = contracts.FirstOrDefault(c => c.ContractNumber == request.ContractNumber);

                if (orgChartContract is null)
                {
                    orgChartContract = await orgClient.CreateContractV2Async(request.OrgChartId, new ApiClients.Org.ApiProjectContractV2
                    {
                        ContractNumber = request.ContractNumber,
                        Name = request.ContractNumber
                    });
                }

                var dbProject = await EnsureDbProjectAsync(project);
                var dbContract = await EnsureDbContract(request, dbProject, orgChartContract);


                await resourcesDb.SaveChangesAsync();

                return new QueryContract(dbContract);
            }

            private async Task<DbProject> EnsureDbProjectAsync(ApiProjectV2 project)
            {
                var dbProject = await resourcesDb.Projects.FirstOrDefaultAsync(p => p.OrgProjectId == project.ProjectId);

                if (dbProject is null)
                {
                    dbProject = new DbProject
                    {
                        DomainId = project.DomainId,
                        Name = project.Name,
                        OrgProjectId = project.ProjectId
                    };
                    await resourcesDb.Projects.AddAsync(dbProject);
                }

                return dbProject;
            }

            private async Task<DbContract> EnsureDbContract(AllocateContract command, DbProject dbProject, ApiProjectContractV2 contract)
            {
                var dbContract = await resourcesDb.Contracts.FirstOrDefaultAsync(c => c.OrgContractId == contract.Id);

                if (dbContract is null)
                {
                    dbContract = new DbContract
                    {
                        ContractNumber = contract.ContractNumber,
                        Name = contract.Name,
                        OrgContractId = contract.Id,
                        ProjectId = dbProject.Id,
                        Allocated = DateTimeOffset.UtcNow,
                        AllocatedBy = command.Editor.Person
                    };
                    await resourcesDb.Contracts.AddAsync(dbContract);
                }

                return dbContract;
            }

            //private async Task ValidateAsync(AllocateContract request)
            //{
            //    project = await orgClient.GetProjectOrDefaultV2Async(request.OrgChartId);
            //    if (project is null)
            //        throw new CommandValidationError($"Could not locate any org chart project with id '{request.OrgChartId}'");

            //    var contracts = await orgClient.GetContractsV2Async(request.OrgChartId);
                
            //    if (contracts.Any(c => c.ContractNumber == request.ContractNumber))
            //    {

            //        //throw new CommandValidationError($"The contract '{request.ContractNumber}' has already been allocated to the project '{project.Name}'");
            //    }
            //}
        }

    }

    public class CommandValidationError : InvalidOperationException
    {
        public CommandValidationError(string message) : base(message)
        {
        }
    }

    //internal class EnsureContract : IRequest<DbContract>
    //{
    //    public EnsureContract(Guid orgChartId, OrgContractId contractId)
    //    {
    //        OrgChartId = orgChartId;
    //        ContractId = contractId;
    //    }
    //    public Guid OrgChartId { get; set; }
    //    public OrgContractId ContractId { get; set; }


    //    public class Handler : IRequestHandler<EnsureContract, DbContract>
    //    {
    //        private readonly IOrgApiClient orgClient;
    //        private readonly ResourcesDbContext resourcesDb;

    //        private ApiClients.Org.ApiProjectV2 project;

    //        public Handler(IOrgApiClientFactory orgApiClientFactory, ResourcesDbContext resourcesDb)
    //        {
    //            orgClient = orgApiClientFactory.CreateClient(ApiClientMode.Application);
    //            this.resourcesDb = resourcesDb;
    //        }

    //        public async Task<DbContract> Handle(EnsureContract request, CancellationToken cancellationToken)
    //        {
    //            project = await orgClient.GetProjectOrDefaultV2Async(request.OrgChartId);

    //            if (project is null)
    //                throw new CommandValidationError($"Could not locate any org chart project with id '{request.OrgChartId}'");


    //            var contracts = await orgClient.GetContractsV2Async(request.OrgChartId);

    //            var orgChartContract = contracts.FirstOrDefault(c => c.ContractNumber == request.ContractNumber);

    //            if (orgChartContract is null)
    //            {
    //                orgChartContract = await orgClient.CreateContractV2Async(request.OrgChartId, new ApiClients.Org.ApiProjectContractV2
    //                {
    //                    ContractNumber = request.ContractNumber,
    //                    Name = request.ContractNumber
    //                });
    //            }

    //            var dbProject = await EnsureDbProjectAsync(project);
    //            var dbContract = await EnsureDbContract(dbProject, orgChartContract);


    //            await resourcesDb.SaveChangesAsync();

    //            return new QueryContract(dbContract);
    //        }

    //        private async Task<DbProject> EnsureDbProjectAsync(ApiProjectV2 project)
    //        {
    //            var dbProject = await resourcesDb.Projects.FirstOrDefaultAsync(p => p.OrgProjectId == project.ProjectId);

    //            if (dbProject is null)
    //            {
    //                dbProject = new DbProject
    //                {
    //                    DomainId = project.DomainId,
    //                    Name = project.Name,
    //                    OrgProjectId = project.ProjectId
    //                };
    //                await resourcesDb.Projects.AddAsync(dbProject);
    //            }

    //            return dbProject;
    //        }

    //        private async Task<DbContract> EnsureDbContract(DbProject dbProject, ApiProjectContractV2 contract)
    //        {
    //            var dbContract = await resourcesDb.Contracts.FirstOrDefaultAsync(c => c.OrgContractId == contract.Id);

    //            if (dbContract is null)
    //            {
    //                dbContract = new DbContract
    //                {
    //                    ContractNumber = contract.ContractNumber,
    //                    Name = contract.Name,
    //                    OrgContractId = contract.Id,
    //                    ProjectId = dbProject.Id
    //                };
    //                await resourcesDb.Contracts.AddAsync(dbContract);
    //            }

    //            return dbContract;
    //        }

    //        //private async Task ValidateAsync(AllocateContract request)
    //        //{
    //        //    project = await orgClient.GetProjectOrDefaultV2Async(request.OrgChartId);
    //        //    if (project is null)
    //        //        throw new CommandValidationError($"Could not locate any org chart project with id '{request.OrgChartId}'");

    //        //    var contracts = await orgClient.GetContractsV2Async(request.OrgChartId);

    //        //    if (contracts.Any(c => c.ContractNumber == request.ContractNumber))
    //        //    {

    //        //        //throw new CommandValidationError($"The contract '{request.ContractNumber}' has already been allocated to the project '{project.Name}'");
    //        //    }
    //        //}
    //    }

    //}

}
