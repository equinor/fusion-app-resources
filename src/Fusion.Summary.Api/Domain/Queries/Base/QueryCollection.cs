using System.Collections;

namespace Fusion.Summary.Api.Domain.Queries.Base;

public class QueryCollection<T> : IEnumerable<T>
{
    private readonly List<T> items;

    public QueryCollection()
    {
        items = new List<T>();
    }

    public QueryCollection(IEnumerable<T> items)
    {
        this.items = items.ToList();
    }

    public QueryCollection(IEnumerable<T> items, int top, int skip, int totalCount)
    {
        this.items = items.ToList();

        Top = top;
        TotalCount = totalCount;
        Skip = skip;
    }


    public int? Top { get; set; }
    public int? TotalCount { get; set; }
    public int? Skip { get; set; }


    public IEnumerator<T> GetEnumerator() => items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();
}