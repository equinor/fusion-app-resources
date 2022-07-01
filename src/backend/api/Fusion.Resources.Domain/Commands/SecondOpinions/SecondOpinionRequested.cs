using MediatR;

namespace Fusion.Resources.Domain
{
    public class SecondOpinionRequested : INotification
    {
        public SecondOpinionRequested(QuerySecondOpinion secondOpinion, QueryResourceAllocationRequest request, QueryPerson person)
        {
            SecondOpinion = secondOpinion;
            Request = request;
            Person = person;
        }

        public QuerySecondOpinion SecondOpinion { get; }
        public QueryResourceAllocationRequest Request { get; }
        public QueryPerson Person { get; }
    }
}
