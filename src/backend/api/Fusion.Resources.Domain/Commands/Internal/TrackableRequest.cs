using Fusion.Resources.Database.Entities;
using MediatR;
using System;

namespace Fusion.Resources.Domain.Commands
{
    public class TrackableRequest<TResponse> : IRequest<TResponse>, ITrackableRequest
    {
        public CommandEditor Editor { get; private set; }

        public void SetEditor(Guid azureUniqueId, DbPerson person)
        {
            Editor = new CommandEditor
            {
                AzureUniqueId = azureUniqueId,
                Person = person
            };
        }
    }

    public class TrackableRequest : IRequest, ITrackableRequest
    {
        public CommandEditor Editor { get; private set; }

        public void SetEditor(Guid azureUniqueId, DbPerson person)
        {
            Editor = new CommandEditor
            {
                AzureUniqueId = azureUniqueId,
                Person = person
            };
        }
    }
}
