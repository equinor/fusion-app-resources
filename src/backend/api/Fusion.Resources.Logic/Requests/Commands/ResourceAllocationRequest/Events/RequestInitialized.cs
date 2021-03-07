using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
using MediatR;
using System;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public class RequestInitialized : INotification
        {
            public RequestInitialized(Guid requestId, InternalRequestType type, DbPerson person)
            {
                RequestId = requestId;
                Type = type;
                InitiatedByDbPersonId = person.Id;
            }

            public Guid RequestId { get; set; }
            public InternalRequestType Type { get; set; }

            public Guid InitiatedByDbPersonId { get; set; }
        }
    }

}