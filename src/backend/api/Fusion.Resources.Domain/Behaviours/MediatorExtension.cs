using Fusion.Resources.Domain.Behaviours;
using MediatR;

namespace Fusion.Resources
{
    public static class MediatorExtension
    {
        public static SystemEditorScope SystemAccountScope(this IMediator mediator)
        {
            return new SystemEditorScope();
        }
    }
}
