using Fusion.Resources.Domain;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiProposedPerson
    {
        public ApiProposedPerson(QueryProposedPerson query)
        {
            this.Person = new ApiPerson(query.Person);
            this.WasNotified = query.WasNotified;
        }

        public ApiPerson Person { get; set; }
        public bool WasNotified { get; set; }
    }
}