using System;

namespace Fusion.Resources.Api.Controllers
{
    public class TaskOwnerReference
    {
        /// <summary>
        /// The position id is nullable, as at a later date other ways of referencing an un-provisioned request will be made available.
        /// </summary>
        public Guid? PositionId { get; set; }
    }

}
