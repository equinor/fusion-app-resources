namespace Fusion.Summary.Api.Controllers.ApiModels;

public class ApiPersonnelMoreThan100PercentFTE
{
    public required Guid Id { get; set; }
    public required string FullName { get; set; }
    public required int FTE { get; set; }
}