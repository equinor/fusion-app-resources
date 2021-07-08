using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Tests
{
    internal class PeopleIntegrationMock : IPeopleIntegration
    {
        public static ConcurrentBag<(Guid azureId, string preferredMail)> Requests { get; } = new ConcurrentBag<(Guid, string)>();

        public Task UpdatePreferredContactMailAsync(Guid azureUniqueId, string preferredContactMail)
        {
            Requests.Add((azureUniqueId, preferredContactMail));
            return Task.CompletedTask;
        }
    }
}
