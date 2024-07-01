using System;
using System.Collections.Generic;

namespace Fusion.Resources.Api
{
    public class LineOrgEventBody
    {
        /// <summary>
        /// SAP id of org unit. Should not change. Primary identifier.
        /// </summary>
        public string SapId { get; set; } = null!;

        /// <summary>
        /// The full department string pre update. This might change in an re-organisation.
        /// </summary>
        public string FullDepartment { get; set; } = null!;

        public string Type { get; set; }
        public List<string> Changes { get; set; } = new List<string>();

        public ChangeType GetChangeType() => Enum.TryParse<ChangeType>(Type, true, out var changeType) ? changeType : ChangeType.Unknown;

        public enum ChangeType { Unknown, Updated, Created, Deleted }
    }
}