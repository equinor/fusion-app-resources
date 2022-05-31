using Fusion.AspNetCore.Api;

namespace Fusion.Resources.Api.Controllers
{
    public class PatchSecondOpinionResponseRequest
    {
        public PatchProperty<string> Comment { get; set; } = new();
        public PatchProperty<string> State { get; set; } = new();
    }
}
