using Fusion.Resources.Domain;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiTaskOwnerWithPerson : TaskOwnerReference
    {
        public ApiTaskOwnerWithPerson(QueryTaskOwnerWithPerson query)
        {
            this.PositionId = query.PositionId;
            this.Person = ApiPerson.FromEntityOrDefault(query.Person);
        }
        public ApiPerson? Person { get; set; }
    }
}