using System;

namespace Fusion.Resources.Database.Entities
{
    public interface ITrackableEntity
    {
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? Updated { get; set; }

        //public Guid CreatedByAzureUniqueId { get; set; }
        //public Guid? UpdatedByAzureUniqueId { get; set; }
    }

}
