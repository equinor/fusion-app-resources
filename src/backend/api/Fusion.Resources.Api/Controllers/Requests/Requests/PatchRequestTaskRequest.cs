using Fusion.AspNetCore.Api;

namespace Fusion.Resources.Api.Controllers
{
    public class PatchRequestTaskRequest : PatchRequest
    {
        public PatchProperty<string> Title { get; set; } = new();
        public PatchProperty<string> Body { get; set; } = new();
        public PatchProperty<string> Category { get; set; } = new();
        public PatchProperty<string> Type { get; set; } = new();
        public PatchProperty<string?> SubType { get; set; } = new();
        public PatchProperty<bool> IsResolved { get; set; } = new();
    }
}
