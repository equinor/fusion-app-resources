using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;

namespace Fusion.Resources.Api.Tests.FusionMocks
{
    class AlwaysEmptyCache : IMemoryCache
    {
        class ExpiredEntry : ICacheEntry
        {
            public ExpiredEntry(object key)
            {
                Key = key;
            }
            public DateTimeOffset? AbsoluteExpiration { get; set; } = DateTimeOffset.MinValue;
            public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; } = DateTimeOffset.Now - DateTimeOffset.MinValue;

            public IList<IChangeToken> ExpirationTokens => new List<IChangeToken>();

            public object Key { get; }

            public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks => new List<PostEvictionCallbackRegistration>();

            public CacheItemPriority Priority { get; set; }
            public long? Size { get; set; }
            public TimeSpan? SlidingExpiration { get; set; }
            public object Value { get; set; }

            public void Dispose()
            {
            }
        }
        public ICacheEntry CreateEntry(object key) => new ExpiredEntry(key);

        public void Dispose(){}

        public void Remove(object key){}

        public bool TryGetValue(object key, out object value)
        {
            value = null;
            return false;
        }
    }
}
