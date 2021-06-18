#nullable enable

using System;
using System.Collections.Generic;

namespace Fusion.Testing.Mocks
{
    public class TestApiTaskOwner 
    {
        public DateTime Date { get; set; }
        public Guid? PositionId { get; set; }
        public List<Guid>? InstanceIds { get; set; }
        public List<TestApiPerson>? Persons { get; set; }
    }
}