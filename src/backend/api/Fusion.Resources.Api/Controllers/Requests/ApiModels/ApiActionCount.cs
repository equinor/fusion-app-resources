using Fusion.Resources.Domain;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiActionCount
    {
        public ApiActionCount(QueryActionCounts actionCount)
        {
            Total = actionCount.TotalCount;
            Resolved = actionCount.ResolvedCount;
            Unresolved = actionCount.UnresolvedCount;
        }

        public int Total { get; }
        public int Resolved { get; }
        public int Unresolved { get; }
    }
}