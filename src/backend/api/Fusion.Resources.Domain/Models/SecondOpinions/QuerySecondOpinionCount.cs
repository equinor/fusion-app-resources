namespace Fusion.Resources.Domain
{
    public class QuerySecondOpinionCount
    {
        public int TotalCount { get; }
        public int PublishedCount { get; }

        public QuerySecondOpinionCount(int totalCount, int publishedCount)
        {
            this.TotalCount = totalCount;
            this.PublishedCount = publishedCount;
        }
    }
}