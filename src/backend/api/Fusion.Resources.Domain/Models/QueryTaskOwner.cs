using System;

namespace Fusion.Resources.Domain
{
    public class QueryTaskOwner
    {
        public Guid? PositionId { get; set; }
        public ApiClients.Org.ApiPersonV2? Person { get; set; }
    }
}