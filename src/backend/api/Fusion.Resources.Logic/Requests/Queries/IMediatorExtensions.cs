using Fusion.Resources.Database.Entities;
using Fusion.Resources.Logic.Queries;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic
{
    public static class IMediatorExtensions
    {

        public static async Task<DbWorkflow> GetRequestWorkflowAsync(this IMediator mediator, Guid requestId)
        {
            return await mediator.Send(new GetRequestWorkflow(requestId));
        }
    }
}
