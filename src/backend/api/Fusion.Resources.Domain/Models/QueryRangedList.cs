using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Resources.Domain
{
    public class QueryRangedList<T> : List<T>
    {
        public int Skip { get; private set; }
        public int TotalCount { get; private set; }
        public int PageSize { get; private set; }

        public bool HasPrevious => Skip > 0;
        public bool HasNext => (Skip + Count) < TotalCount;

        public QueryRangedList(IEnumerable<T> items, int total, int skip)
        {
            TotalCount = total;
            Skip = skip;
            AddRange(items);
            PageSize = Count;
        }

        public QueryRangedList(int pageCount, int total, int skip)
        {
            TotalCount = total;
            Skip = skip;
            PageSize = pageCount;
        }

        public static async Task<QueryRangedList<TResult>> FromQueryAsync<TResult>(IQueryable<TResult> source, int skip, int take, bool includeData = false)
        {
            var count = await source.CountAsync();

            if (includeData)
            {
                var items = await source.Skip(skip).Take(take).ToListAsync();
                return new QueryRangedList<TResult>(items, count, skip);
            }

            var pageCount = await source.Skip(skip).Take(take).CountAsync();
            return new QueryRangedList<TResult>(pageCount, count, skip);
        }
    }

    /// <summary>
    /// Anchor point for the static method that created new collection based on type inference.
    /// </summary>
    public partial class QueryRangedList
    {
        public static async Task<QueryRangedList<T>> FromQueryAsync<T>(IQueryable<T> source, int skip, int take, bool skipDataLoad = false)
        {
            var count = await source.CountAsync();

            if (skipDataLoad == false)
            {
                var items = await source.Skip(skip).Take(take).ToListAsync();
                return new QueryRangedList<T>(items, count, skip);
            }

            var pageCount = await source.Skip(skip).Take(take).CountAsync();
            return new QueryRangedList<T>(pageCount, count, skip);
        }

        public static QueryRangedList<T> FromItems<T>(IEnumerable<T> items, int totalCount, int skip)
        {
            return new QueryRangedList<T>(items, totalCount, skip);
        }

        public static QueryRangedList<T> FromEnumerableItems<T>(IEnumerable<T> source, int skip, int take, bool skipDataLoad = false)
        {
            var count = source.Count();

            if (skipDataLoad == false)
            {
                var items = source.Skip(skip).Take(take);
                return new QueryRangedList<T>(items, count, skip);
            }
            var pageCount = source.Skip(skip).Take(take).Count();
            return new QueryRangedList<T>(pageCount, count, skip);
        }
    }
}