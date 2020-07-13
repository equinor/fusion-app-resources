using Fusion.Resources.Database.Entities;
using MediatR;
using System;

namespace Fusion.Resources.Domain.Commands
{
    /// <summary>
    /// The trackable requests automatically includes the user that performes the operation. 
    /// This is fetched from the HttpContext, currently signed in user. 
    /// 
    /// If the user should be manually set, set the Editor property before dispatching the request.
    /// </summary>
    public class TrackableRequest<TResponse> : IRequest<TResponse>, ITrackableRequest
    {
        public CommandEditor Editor { get; private set; } = null!;

        public void SetEditor(Guid azureUniqueId, DbPerson person)
        {
            Editor = new CommandEditor(azureUniqueId, person);
        }
    }

    /// <summary>
    /// The trackable requests automatically includes the user that performes the operation. 
    /// This is fetched from the HttpContext, currently signed in user. 
    /// 
    /// If the user should be manually set, set the Editor property before dispatching the request.
    /// </summary>
    public class TrackableRequest : IRequest, ITrackableRequest
    {
        public CommandEditor Editor { get; private set; } = null!;

        public void SetEditor(Guid azureUniqueId, DbPerson person)
        {
            Editor = new CommandEditor(azureUniqueId, person);
        }
    }
}
