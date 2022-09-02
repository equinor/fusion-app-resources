namespace Fusion.Resources.Domain
{
    public class QuerySecondOpinionCounts
    {
        public int TotalCount { get; }
        public int PublishedCount { get; }

        public QuerySecondOpinionCounts(int totalCount, int publishedCount)
        {
            this.TotalCount = totalCount;
            this.PublishedCount = publishedCount;
        }
    }
}