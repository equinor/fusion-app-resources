namespace Fusion.Summary.Api.Controllers;

public record PutDepartmentRequest(string FullDepartmentName, Guid ResourceOwnerAzureUniqueId);