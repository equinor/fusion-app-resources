using Fusion.Resources.Domain;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiSecondOpinionCounts
    {
        public ApiSecondOpinionCounts(QuerySecondOpinionCounts secondOpinionCounts)
        {
            TotalCount = secondOpinionCounts.TotalCount;
            PublishedCount = secondOpinionCounts.PublishedCount;
        }

        public int TotalCount { get; }
        public int PublishedCount { get; }
    }
}
