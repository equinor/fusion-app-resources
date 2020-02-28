using System;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiRequestPosition
    {
        public Guid? Id { get; set; }
        public string ExternalId { get; set; }
        public string Name { get; set; }
        public DateTime AppliesFrom { get; set; }
        public DateTime AppliesTo { get; set; }

        public ApiRequestBasePosition BasePosition { get; set; }
        public ApiRequestTaskOwner TaskOwner { get; set; }
    }
}
