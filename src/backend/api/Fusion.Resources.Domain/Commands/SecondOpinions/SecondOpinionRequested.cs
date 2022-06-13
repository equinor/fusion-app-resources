using MediatR;

namespace Fusion.Resources.Domain
{
    public class SecondOpinionRequested : INotification
    {
        public SecondOpinionRequested(QueryResourceAllocationRequest request, QueryPerson person)
        {
            Request = request;
            Person = person;
        }

        public QueryResourceAllocationRequest Request { get; }
        public QueryPerson Person { get; }
    }
}
