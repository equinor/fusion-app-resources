namespace Fusion.Summary.Api.Controllers.ApiModels;

public class ApiEndingPosition
{
    public required Guid Id { get; set; }
    public required string FullName { get; set; }
    public required DateTime EndDate { get; set; }
}