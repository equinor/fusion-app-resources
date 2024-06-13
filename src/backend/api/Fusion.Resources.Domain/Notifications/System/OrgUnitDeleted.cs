using Fusion.Resources.Domain.Behaviours;
using MediatR;

namespace Fusion.Resources.Domain.Notifications.System
{
    /// <summary>
    /// Event that indicates that an org unit has been deleted in the master data (line org which is mirroring SAP).
    /// </summary>
    public partial class OrgUnitDeleted : INotification
    {
        public OrgUnitDeleted(string sapId, string fullDepartment)
        {
            SapId = sapId;
            FullDepartment = fullDepartment;
        }

        public string SapId { get; private set; }
        public string FullDepartment { get; private set; }
    }
}
