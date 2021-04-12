using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Fusion.Resources.Application.LineOrg
{
    class DepartmentCache : KeyedCollection<string, DepartmentCacheItem>
    {
        private static TimeSpan CacheDuration = TimeSpan.FromDays(1);
        private DateTime? Expiry = null;
        public bool IsValid => DateTime.Now < Expiry;

        protected override void SetItem(int index, DepartmentCacheItem item)
            => base.SetItem(index, item with { Expiry = ItemExpiry() });

        protected override void InsertItem(int index, DepartmentCacheItem item)
            => base.InsertItem(index, item with { Expiry = ItemExpiry() });

        protected override void ClearItems()
        {
            Expiry = DateTime.MaxValue;
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
            Expiry = (Expiry is null || itemExpiry < Expiry) ? itemExpiry : Expiry;
            return itemExpiry;
        }
    }

    record DepartmentCacheItem(string DepartmentId, string SearchText, Guid LineOrgResponsibleId)
    {
        internal DateTime? Expiry = null;
    }
}
