using System;
using System.Text.Json.Serialization;
using Fusion.Integration.Profile;
using Fusion.Resources.Domain.Models;

namespace Fusion.Resources.Api.Controllers;

public class ApiPersonDelegatedResponsibility 
{
    public ApiPersonDelegatedResponsibility(QueryDepartmentResponsible responsible)
    {
        AzureUniquePersonId = responsible.DelegatedResponsible?.AzureUniqueId;
        Mail = responsible.DelegatedResponsible?.Mail;
        Name = responsible.DelegatedResponsible?.Name;
        PhoneNumber = responsible.DelegatedResponsible?.MobilePhone;
        JobTitle = responsible.DelegatedResponsible?.JobTitle;
        FullDepartment = responsible.DelegatedResponsible?.FullDepartment;
        AccountType = responsible.DelegatedResponsible?.AccountType;
        DateFrom = responsible.DateFrom;
        DateTo=responsible.DateTo;
        CreatedDate = responsible.CreatedDate;
        if (responsible.CreatedBy != null) 
            CreatedBy = new ApiPerson(responsible.CreatedBy);
        Reason = responsible.Reason;
    }

    public Guid? AzureUniquePersonId { get; set; }
    public string? Mail { get; set; } = null!;
    public string? Name { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? JobTitle { get; set; }
    public string? FullDepartment { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FusionAccountType? AccountType { get; set; }
    public DateTimeOffset? DateFrom { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? DateTo { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? CreatedDate { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ApiPerson? CreatedBy { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Reason { get; set; }
}