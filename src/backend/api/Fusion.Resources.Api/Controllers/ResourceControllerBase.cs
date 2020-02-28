using Fusion.Integration.Profile;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Controllers
{
    public class ResourceControllerBase : ControllerBase
    {
        public FusionFullPersonProfile? UserFusionProfile
        {
            get
            {
                if (HttpContext.Items.ContainsKey("FusionProfile"))
                {
                    var profile = HttpContext.Items["FusionProfile"] as FusionFullPersonProfile;

                    if (profile != null && profile.Roles != null)
                    {
                        return profile;
                    }
                }

                return null;
            }
        }
    }
}
