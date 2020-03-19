using Fusion.Resources.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fusion.Resources.Database
{
    public static class ContractorRequestQueries
    {
        public static IQueryable<DbContractorRequest> IsRunningQuery(this IQueryable<DbContractorRequest> items)
        {
            return items.Where(r => r.State == DbRequestState.Created || r.State == DbRequestState.SubmittedToCompany);
        }

    }
}
