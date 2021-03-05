using System;
using System.Linq;
using Fusion.Resources.Domain;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiTaskOwner 
    {
        public ApiTaskOwner(QueryTaskOwner query)
        {
            Date = query.Date;
            PositionId = query.PositionId;
            InstanceIds = query.InstanceIds;
            Persons = query.Persons?.Select(p => ApiPerson.FromEntityOrDefault(p)!).ToArray();
        }

        public DateTime Date { get; set; }
        public Guid? PositionId { get; set; }
        public Guid[]? InstanceIds { get; set; }
        public ApiPerson[]? Persons { get; set; }
    }
}