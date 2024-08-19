using System;
using System.Collections.Generic;
using Fusion.Integration.Profile;

namespace Fusion.Testing.Mocks.ProfileService.Api;

public class ApiFusionApplicationProfile
{
    public Guid AzureUniqueId => ServicePrincipalId;
    public Guid ServicePrincipalId { get; set; }

    public string Name => DisplayName;
    public string DisplayName { get; set; } = null!;
    public Guid ApplicationId { get; set; }

    public List<FusionRole>? Roles { get; set; }
}