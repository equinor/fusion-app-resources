using System;

namespace Fusion.Resources.Domain.Services.OrgClient.Models;

public class ApiContractReferenceV2
{
    public Guid Id { get; set; }
    public string ContractNumber { get; set; } = null!;
    public string Name { get; set; } = null!;
}