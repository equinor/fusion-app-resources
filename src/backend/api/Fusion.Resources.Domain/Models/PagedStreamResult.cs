using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Models;

public class PagedStreamResult<T>
{
    public int TotalCount { get; }
    public int Top { get; }
    public int Skip { get; }
    public IAsyncEnumerable<T> Items { get; }

    public PagedStreamResult(int totalCount, int top, int skip, IAsyncEnumerable<T> items)
    {
        TotalCount = totalCount;
        Top = top;
        Skip = skip;
        Items = items;
    }
}
