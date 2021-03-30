using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Database.Entities;
using Newtonsoft.Json;
using System;

namespace Fusion.Resources.Logic.Tests
{
    public static class DbResourceAllocationRequestExtensions
    {
        public static DbResourceAllocationRequest AsResourceRemoval(this DbResourceAllocationRequest req)
        {
            req.SubType = "removeResource";
            return req;
        }

        public static DbResourceAllocationRequest AsResourceChange(this DbResourceAllocationRequest req, ApiPersonProfileV3 pers)
        {
            req.SubType = "changeResource";
            return req.WithProposedPerson(pers);
        }

        public static DbResourceAllocationRequest AsAdjustment(this DbResourceAllocationRequest req, object changes = null)
        {
            req.SubType = "adjustment";

            if (changes != null)
                req.WithProposedChanges(changes);

            return req;
        }

        public static DbResourceAllocationRequest WithChangeDate(this DbResourceAllocationRequest req, DateTime changeDate)
        {
            req.ProposalParameters.ChangeFrom = changeDate.Date;
            return req;
        }

        public static DbResourceAllocationRequest WithChangeRange(this DbResourceAllocationRequest req, DateTime from, DateTime to)
        {
            req.ProposalParameters.ChangeFrom = from.Date;
            req.ProposalParameters.ChangeTo = to.Date;
            return req;
        }

        public static DbResourceAllocationRequest WithProposedChanges(this DbResourceAllocationRequest req, object changes)
        {
            req.ProposedChanges = JsonConvert.SerializeObject(changes);
            return req;
        }

        public static DbResourceAllocationRequest WithProposedPerson(this DbResourceAllocationRequest req, ApiPersonProfileV3 person)
        {
            req.ProposedPerson = new DbResourceAllocationRequest.DbOpProposedPerson()
            {
                AzureUniqueId = person.AzureUniqueId.Value,
                HasBeenProposed = true,
                Mail = person.Mail,
                ProposedAt = DateTime.UtcNow,
                WasNotified = false
            };
            return req;
        }
    }

}
