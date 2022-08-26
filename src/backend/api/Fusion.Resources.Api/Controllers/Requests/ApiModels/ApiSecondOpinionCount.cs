using Fusion.Resources.Domain;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiSecondOpinionCount
    {
        public ApiSecondOpinionCount(QuerySecondOpinionCount secondOpinionCounts)
        {
            TotalCount = secondOpinionCounts.TotalCount;
            PublishedCount = secondOpinionCounts.PublishedCount;
        }

        public int TotalCount { get; }
        public int PublishedCount { get; }
    }
}
