using Fusion.Resources.Domain.Models;
using System;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiRequestStatus
    {
        public ApiRequestStatus(QueryRequestStatus requestStatus)
        {
            Id = requestStatus.Id;
            State = requestStatus.State;
            IsDraft = requestStatus.IsDraft;
        }

        public Guid Id { get; }
        public string? State { get; }
        public bool IsDraft { get; }
    }
}
