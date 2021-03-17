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
            public RequestInitialized(Guid requestId, DbInternalRequestType type, string? subType, DbPerson person)
            {
                RequestId = requestId;
                Type = type;
                SubType = subType;
                InitiatedByDbPersonId = person.Id;
            }

            public Guid RequestId { get; set; }
            public DbInternalRequestType Type { get; set; }
            public string? SubType { get; set; }
            public Guid InitiatedByDbPersonId { get; set; }
        }
    }

}