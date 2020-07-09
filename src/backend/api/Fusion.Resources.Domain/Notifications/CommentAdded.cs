using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications
{
    public class CommentAdded : INotification
    {
        public CommentAdded(Guid commentId)
        {
            CommentId = commentId;
        }

        public Guid CommentId { get; }
    }
}
