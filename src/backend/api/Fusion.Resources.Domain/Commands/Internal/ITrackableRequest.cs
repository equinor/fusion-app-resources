using Fusion.Resources.Database.Entities;
using System;

namespace Fusion.Resources.Domain.Commands
{
    public interface ITrackableRequest
    {
        CommandEditor Editor { get; }

        void SetEditor(Guid azureUniqueId, DbPerson person);
    }
}
