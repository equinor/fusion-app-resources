using System;

namespace Fusion.Resources.Domain
{
    public class QueryTaskOwnerWithPerson
    {
        public Guid? PositionId { get; set; }
        public QueryPerson? Person { get; set; }
    }
}