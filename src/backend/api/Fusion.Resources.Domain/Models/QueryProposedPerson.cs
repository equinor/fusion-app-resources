using Fusion.Resources.Database.Entities;

namespace Fusion.Resources.Domain
{
    public class QueryProposedPerson
    {
        public QueryProposedPerson(DbPerson person)
        {
            Person = new QueryPerson(person);
            WasNotified = person.WasNotified;
        }

        public QueryPerson Person { get; set; }
        public bool WasNotified { get;set; }
    }
}
