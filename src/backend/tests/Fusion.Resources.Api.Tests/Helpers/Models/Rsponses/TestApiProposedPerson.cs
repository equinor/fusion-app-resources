using System;

#nullable enable

namespace Fusion.Testing.Mocks
{
    public class TestApiProposedPerson
    {
        public DateTimeOffset ProposedAt { get; set; }
        public TestApiPerson Person { get; set; } = null!;
        public bool WasNotified { get; set; }
    }


}
