using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Fusion.Resources.Application.LineOrg
{
    class DepartmentCache : KeyedCollection<string, DepartmentCacheItem>
    {
        private static TimeSpan CacheDuration = TimeSpan.FromDays(1);
        private DateTime? firstExpiry = null;
        public bool IsValid => firstExpiry.HasValue && DateTime.Now < firstExpiry;

        protected override void SetItem(int index, DepartmentCacheItem item)
            => base.SetItem(index, item with { Expiry = ItemExpiry() });

        protected override void InsertItem(int index, DepartmentCacheItem item)
            => base.InsertItem(index, item with { Expiry = ItemExpiry() });

        protected override void ClearItems()
        {
            firstExpiry = null;
            base.ClearItems();
        }

        protected override string GetKeyForItem(DepartmentCacheItem item) => item.DepartmentId;
        public IEnumerable<DepartmentCacheItem> Search(string? search)
        {
            if (string.IsNullOrEmpty(search)) return this;

            return this.Where(itm => itm.SearchText.Contains(search, StringComparison.InvariantCultureIgnoreCase));
        }

        private DateTime ItemExpiry()
        {
            var itemExpiry = DateTime.Now.Add(CacheDuration);
            firstExpiry = (firstExpiry is null || itemExpiry < firstExpiry) ? itemExpiry : firstExpiry;
            return itemExpiry;
        }
    }

    record DepartmentCacheItem(string DepartmentId, string SearchText, Guid LineOrgResponsibleId)
    {
        internal DateTime? Expiry = null;
    }
}
