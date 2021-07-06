using Fusion.Testing.Mocks;
using System;
using System.Collections.Generic;

namespace Fusion.Resources.Api.Tests
{
    public class TestApiRequestTask
    {
        public Guid id { get; set; }
        public string title { get; set; }
        public string body { get; set; }
        public string type { get; set; }
        public string subType { get; set; }
        public string source { get; set; }
        public string responsible { get; set; }
        public bool isResolved { get; set; }
        public DateTimeOffset? resolvedAt { get; set; }
        public TestApiPerson resolvedBy { get; set; }

        public Dictionary<string, object> properties { get; set; }
    }
}
