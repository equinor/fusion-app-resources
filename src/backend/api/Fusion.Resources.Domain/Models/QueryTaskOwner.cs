using System;

namespace Fusion.Resources.Domain
{
    public class QueryTaskOwner
    {
        public QueryTaskOwner(DateTime date)
        {
            Date = date;
        }

        /// <summary>
        /// The applicable date used to resolve the task owner.
        /// </summary>
        public DateTime Date {get;set;}
        public Guid? PositionId { get; set; }
        public Guid[]? InstanceIds { get; set; }

        public ApiClients.Org.ApiPersonV2[]? Persons { get; set; }
    }
}