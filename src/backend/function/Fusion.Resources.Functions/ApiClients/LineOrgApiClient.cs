﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fusion.Resources.Functions.ApiClients.ApiModels;
using Fusion.Resources.Functions.Integration;

namespace Fusion.Resources.Functions.ApiClients;

public class LineOrgApiClient : ILineOrgApiClient
{
    private readonly HttpClient lineOrgClient;

    public LineOrgApiClient(IHttpClientFactory httpClientFactory)
    {
        lineOrgClient = httpClientFactory.CreateClient(HttpClientNames.Application.LineOrg);
        lineOrgClient.Timeout = TimeSpan.FromMinutes(5);
    }

    public async Task<IEnumerable<OrgUnits>> GetOrgUnitDepartmentsAsync()
    {
        var data =
            await lineOrgClient.GetAsJsonAsync<InternalCollection<OrgUnits>>($"/org-units?$top={int.MaxValue}");

        return data.Value
            .Where(x => !string.IsNullOrEmpty(x.FullDepartment))
            .ToList();
    }

    public async Task<List<LineOrgPerson>> GetResourceOwnersFromFullDepartment(List<OrgUnits> fullDepartments)
    {
        var list = fullDepartments
            .Select(l => $"'{l.FullDepartment?.Replace("&", "%26")}'")
            .ToList()
            .Aggregate((a, b) => $"{a}, {b}");
        var queryString = $"/lineorg/persons?$filter=fullDepartment in " +
                          $"({list}) " +
                          $"and isResourceOwner eq 'true'";
        var resourceOwners = await lineOrgClient.GetAsJsonAsync<LineOrgPersonsResponse>(queryString);
        foreach (var r in resourceOwners.Value)
            r.DepartmentSapId = fullDepartments.FirstOrDefault(x => x.FullDepartment == r.FullDepartment)?.SapId;

        return resourceOwners.Value;
    }

    public class OrgUnits
    {
        public string? FullDepartment { get; set; }
        public string? SapId { get; set; }
    }

    internal class InternalCollection<T>
    {
        public InternalCollection(IEnumerable<T> items)
        {
            Value = items;
        }

        public IEnumerable<T> Value { get; set; }
    }
}