using Fusion.Resources.Database;
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

    public class UpdateContract : IRequest<QueryContract>
    {
        public UpdateContract(Guid orgProjectId, Guid orgContractId)
        {
            OrgProjectId = orgProjectId;
            OrgContractId = orgContractId;
        }

        public Guid OrgProjectId { get; set; }
        public Guid OrgContractId { get; set; }

        public MonitorableProperty<string> Name { get; set; } = new MonitorableProperty<string>();
        public MonitorableProperty<string> Description { get; set; } = new MonitorableProperty<string>();
        public MonitorableProperty<DateTime?> StartDate { get; set; } = new MonitorableProperty<DateTime?>();
        public MonitorableProperty<DateTime?> EndDate { get; set; } = new MonitorableProperty<DateTime?>();
        public MonitorableProperty<Guid?> CompanyId { get; set; } = new MonitorableProperty<Guid?>();


        public class Handler : IRequestHandler<UpdateContract, QueryContract>
        {
            private readonly IOrgApiClient orgClient;
            private readonly ResourcesDbContext resourcesDb;

            public Handler(IOrgApiClientFactory orgApiClientFactory, ResourcesDbContext resourcesDb)
            {
                orgClient = orgApiClientFactory.CreateClient(ApiClientMode.Application);
                this.resourcesDb = resourcesDb;
            }

            public async Task<QueryContract> Handle(UpdateContract request, CancellationToken cancellationToken)
            {
                var contracts = await orgClient.GetContractsV2Async(request.OrgProjectId);

                var contract = contracts.FirstOrDefault(c => c.Id == request.OrgContractId);

                if (contract is null)
                    throw new ArgumentException($"Could not locate contract by id '{request.OrgContractId}'");

                var dbContract = await resourcesDb.Contracts.FirstOrDefaultAsync(c => c.OrgContractId == request.OrgContractId);

                if (dbContract is null)
                    throw new InvalidOperationException($"The contract with org id '{request.OrgContractId}' could not be located locally. Has the contract been allocated?");


                if (request.Name.HasBeenSet)
                {
                    contract.Name = request.Name.Value;
                    dbContract.Name = request.Name.Value;
                }
                 
                if (request.CompanyId.HasBeenSet)
                    contract.Company = request.CompanyId.Value.HasValue ? new ApiClients.Org.ApiCompanyV2 { Id = request.CompanyId.Value.Value } : null;                    

                if (request.Description.HasBeenSet) { contract.Description = request.Description.Value; }
                if (request.StartDate.HasBeenSet) { contract.StartDate = request.StartDate.Value; }
                if (request.EndDate.HasBeenSet) { contract.EndDate = request.EndDate.Value; }


                await orgClient.UpdateContractV2Async(request.OrgProjectId, contract);
                await resourcesDb.SaveChangesAsync();

                return new QueryContract(dbContract);
            }
        }

    }


}
