using System;
using Fusion.Resources.Domain;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiTaskOwnerWithPerson 
    {
        public ApiTaskOwnerWithPerson(QueryTaskOwnerWithPerson query)
        {
            this.PositionId = query.PositionId;
            this.Person = ApiPerson.FromEntityOrDefault(query.Person);
        }
        public Guid? PositionId { get; set; }
        public ApiPerson? Person { get; set; }
    }
}