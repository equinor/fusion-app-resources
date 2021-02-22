using Fusion.Resources.Domain.Commands;
using System;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public partial class Normal
        {
            public class Provision : TrackableRequest
            {
                public Provision(Guid requestId)
                {
                    RequestId = requestId;
                }

                public Guid RequestId { get; }

            }
        }
    }
}
