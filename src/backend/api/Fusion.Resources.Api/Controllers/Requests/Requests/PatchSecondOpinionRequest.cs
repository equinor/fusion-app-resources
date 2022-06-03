using Fusion.AspNetCore.Api;
using System.Collections.Generic;

namespace Fusion.Resources.Api.Controllers
{
    public class PatchSecondOpinionRequest : PatchRequest
    {
        public PatchProperty<string> Description { get; set; } = new();
        public PatchProperty<List<PersonReference>> AssignedTo { get; set; } = new();
    }
}
