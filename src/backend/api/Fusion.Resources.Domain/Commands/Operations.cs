using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fusion.Resources
{
    public static class Commands
    {
        public static class Requests
        {
            public static UpdateContractPersonnelRequest UpdateState(Guid requestId, DbRequestState state) => new UpdateContractPersonnelRequest(requestId)
            {
                State = state
            };
        }
    }
}
