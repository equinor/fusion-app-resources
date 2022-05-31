using Fusion.AspNetCore.Api;
using System.Collections.Generic;

namespace Fusion.Resources.Api.Controllers
{
    public class PatchSecondOpinionRequest
    {
        public PatchProperty<string> Description { get; set; }
        public PatchProperty<List<PersonReference>> AssignedTo { get; set; }
    }
}
