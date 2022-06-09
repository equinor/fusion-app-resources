using System;
using System.Collections.Generic;

namespace Fusion.Testing.Mocks
{
    public class TestApiRequestMessage
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string Category { get; set; }

        public string Recipient { get; set; }

        public Guid SenderId { get; set; }
        public TestApiPerson Sender { get; set; }
        public DateTimeOffset Sent { get; set; }

        public Guid RequestId { get; set; }

        public Dictionary<string,object> Properties { get; set; }
    }
}
