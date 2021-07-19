using Fusion.Resources.Api.Controllers;
using Fusion.Resources.Domain;
using System;

namespace Fusion.Resources.Api
{
    public static class EnumExtensions
    {
        public static QueryMessageRecipient ToDomain(this ApiMessageRecipient value)
        {
            return value switch
            {
                ApiMessageRecipient.ResourceOwner => QueryMessageRecipient.ResourceOwner,
                ApiMessageRecipient.TaskOwner => QueryMessageRecipient.TaskOwner,
                _ => throw new NotSupportedException($"Recipient type '{value}' is not supported.")
            };
        }
    }
}
