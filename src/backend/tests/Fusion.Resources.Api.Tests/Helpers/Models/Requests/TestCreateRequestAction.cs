using System;
using System.Collections.Generic;

namespace Fusion.Resources.Api.Tests.Helpers.Models.Requests
{
    class TestCreateRequestAction
    {
        public string title { get; set; }
        public string body { get; set; }
        public string type { get; set; }
        public string subType { get; set; }
        public string source { get; set; }
        public string responsible { get; set; }
    
        public bool isRequired { get; set; }
        public Dictionary<string, object> properties { get; set; }
        public DateTime? dueDate { get; set; }

        public Guid? assignedToId { get; set; }
    }
}
