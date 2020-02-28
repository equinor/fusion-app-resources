using System;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiProjectReference
    {
        public Guid Id { get; set; }
        public Guid ProjectMasterId { get; set; }
        public string Name { get; set; }
    }
}
