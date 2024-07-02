using Newtonsoft.Json;

namespace Fusion.Resources.Functions.Common.ApiClients.ApiModels;

public class LineOrgPersonsResponse
{
    [JsonProperty("totalCount")] public int TotalCount { get; set; }

    [JsonProperty("count")] public int Count { get; set; }

    [JsonProperty("@nextPage")] public object NextPage { get; set; }

    [JsonProperty("value")] public List<LineOrgPerson> Value { get; set; }
}
public class Manager
{
    [JsonProperty("azureUniqueId")] public string AzureUniqueId { get; set; }

    [JsonProperty("mail")] public string Mail { get; set; }

    [JsonProperty("department")] public string Department { get; set; }

    [JsonProperty("fullDepartment")] public string FullDepartment { get; set; }

    [JsonProperty("name")] public string Name { get; set; }

    [JsonProperty("jobTitle")] public string JobTitle { get; set; }
}
public class LineOrgPerson
{
    public string? DepartmentSapId { get; set; }
    [JsonProperty("azureUniqueId")] public string AzureUniqueId { get; set; }

    [JsonProperty("managerId")] public string ManagerId { get; set; }

    [JsonProperty("manager")] public Manager Manager { get; set; }

    [JsonProperty("department")] public string Department { get; set; }

    [JsonProperty("fullDepartment")] public string FullDepartment { get; set; }

    [JsonProperty("name")] public string Name { get; set; }

    [JsonProperty("givenName")] public string GivenName { get; set; }

    [JsonProperty("surname")] public string Surname { get; set; }

    [JsonProperty("jobTitle")] public string JobTitle { get; set; }

    [JsonProperty("mail")] public string Mail { get; set; }

    [JsonProperty("country")] public string Country { get; set; }

    [JsonProperty("phone")] public string Phone { get; set; }

    [JsonProperty("officeLocation")] public string OfficeLocation { get; set; }

    [JsonProperty("userType")] public string UserType { get; set; }

    [JsonProperty("isResourceOwner")] public bool IsResourceOwner { get; set; }

    [JsonProperty("hasChildPositions")] public bool HasChildPositions { get; set; }

    [JsonProperty("hasOfficeLicense")] public bool HasOfficeLicense { get; set; }

    [JsonProperty("created")] public DateTime Created { get; set; }

    [JsonProperty("updated")] public DateTime Updated { get; set; }

    [JsonProperty("lastSyncDate")] public DateTime LastSyncDate { get; set; }
}
