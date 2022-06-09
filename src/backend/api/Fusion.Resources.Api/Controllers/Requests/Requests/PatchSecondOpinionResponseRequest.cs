using Fusion.AspNetCore.Api;

namespace Fusion.Resources.Api.Controllers
{
    public class PatchSecondOpinionResponseRequest : PatchRequest
    {
        public PatchProperty<string> Comment { get; set; } = new();
        public PatchProperty<ApiSecondOpinionResponseStates> State { get; set; } = new();
    }
}
