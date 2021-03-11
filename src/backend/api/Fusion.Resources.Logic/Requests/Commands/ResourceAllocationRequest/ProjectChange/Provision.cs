using Fusion.ApiClients.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {

        public partial class ProjectChange
        {
            public class Provision : IRequest
            {
                public Provision(Guid requestId)
                {
                    RequestId = requestId;
                }

                public Guid RequestId { get; set; }


                public class Handler : AsyncRequestHandler<Provision>
                {
                    private readonly ResourcesDbContext dbContext;
                    private readonly IOrgApiClient client;

                    public Handler(ResourcesDbContext dbContext, IOrgApiClientFactory factory)
                    {
                        this.dbContext = dbContext;
                        client = factory.CreateClient(ApiClientMode.Application);
                    }

                    protected override async Task Handle(Provision request, CancellationToken cancellationToken)
                    {
                        var dbRequest = await dbContext.ResourceAllocationRequests
                            .Include(r => r.Project)
                            .FirstAsync(r => r.Id == request.RequestId);

                        await ProvisionAsync(dbRequest);

                    }


                    private async Task ProvisionAsync(DbResourceAllocationRequest dbRequest)
                    {
                        // Need to get the raw position, in case model has changed.. When putting position, fields not 
                        var rawPosition = await client.GetAsync<JObject>($"/projects/{dbRequest.Project.OrgProjectId}/positions/{dbRequest.OrgPositionId}?api-version=2.0");

                        var position = rawPosition.Value.ToObject<ApiPositionV2>();
                        var positionInstance = position.Instances.First(i => i.Id == dbRequest.OrgPositionInstance.Id);

                        var isFuture = positionInstance.AppliesFrom >= DateTime.UtcNow.Date;

                        if (isFuture)
                            await UpdateFutureSplitAsync(dbRequest, rawPosition.Value);
                        else
                            await UpdateCurrentSplitAsync(dbRequest, rawPosition.Value);
                    }


                    private async Task UpdateFutureSplitAsync(DbResourceAllocationRequest dbRequest, JObject rawPosition)
                    {
                        // Update existing 
                        var instances = rawPosition.GetPropertyCollection<ApiPositionV2>(p => p.Instances)!;

                        var instancePatchRequest = new JObject();

                        var proposedChanges = new JObject();
                        if (!string.IsNullOrEmpty(dbRequest.ProposedChanges))
                            proposedChanges = JObject.Parse(dbRequest.ProposedChanges);

                        if (dbRequest.ProposedPerson.AzureUniqueId != null)
                            instancePatchRequest.SetPropertyValue<ApiPositionInstanceV2>(i => i.AssignedPerson, new ApiPersonV2() { AzureUniqueId = dbRequest.ProposedPerson.AzureUniqueId });

                        if (proposedChanges.TryGetValue("obs", StringComparison.InvariantCultureIgnoreCase, out var obs))
                            instancePatchRequest.SetPropertyValue<ApiPositionInstanceV2>(i => i.Obs, obs);

                        if (proposedChanges.TryGetValue("workload", StringComparison.InvariantCultureIgnoreCase, out var workload))
                            instancePatchRequest.SetPropertyValue<ApiPositionInstanceV2>(i => i.Workload!, workload);

                        if (proposedChanges.TryGetValue("appliesFrom", StringComparison.InvariantCultureIgnoreCase, out var appliesFrom))
                            instancePatchRequest.SetPropertyValue<ApiPositionInstanceV2>(i => i.AppliesFrom, appliesFrom);

                        if (proposedChanges.TryGetValue("appliesTo", StringComparison.InvariantCultureIgnoreCase, out var appliesTo))
                            instancePatchRequest.SetPropertyValue<ApiPositionInstanceV2>(i => i.AppliesTo, appliesTo);

                        if (proposedChanges.TryGetValue("location", StringComparison.InvariantCultureIgnoreCase, out var location))
                            instancePatchRequest.SetPropertyValue<ApiPositionInstanceV2>(i => i.Location, location.ToObject<ApiPositionLocationV2>()!);


                        var url = $"/projects/{dbRequest.Project.OrgProjectId}/positions/{dbRequest.OrgPositionId}/instances/{dbRequest.OrgPositionInstance.Id}?api-version=2.0";
                        var updateResp = await client.PatchAsync<ApiPositionInstanceV2>(url, instancePatchRequest);

                        if (!updateResp.IsSuccessStatusCode)
                            throw new OrgApiError(updateResp.Response, updateResp.Content);
                    }

                    private async Task UpdateCurrentSplitAsync(DbResourceAllocationRequest dbRequest, JObject rawPosition)
                    {
                        // Update existing 
                        var instances = rawPosition.GetPropertyCollection<ApiPositionV2>(p => p.Instances)!;


                        var originalInstanceEndDate = dbRequest.ApplicableChangeDate!.Value.Date;
                        var newInstanceStartDate = originalInstanceEndDate.AddDays(1);

                        // Update the instance we are targeting to end at the applicable date
                        var instanceToUpdate = instances.Cast<JObject>().First(i => i.GetPropertyValue<ApiPositionInstanceV2, Guid>(p => p.Id) == dbRequest.OrgPositionInstance.Id);
                        var newInstance = new JObject(instanceToUpdate);

                        // Stop the current instance at the applicable date
                        instanceToUpdate.SetPropertyValue<ApiPositionInstanceV2>(i => i.AppliesTo, originalInstanceEndDate);
                        newInstance.SetPropertyValue<ApiPositionInstanceV2>(i => i.AppliesFrom, newInstanceStartDate);

                        newInstance.RemoveProperty(nameof(ApiPositionInstanceV2.Id), nameof(ApiPositionInstanceV2.ExternalId));


                        var proposedChanges = new JObject();

                        if (!string.IsNullOrEmpty(dbRequest.ProposedChanges))
                            proposedChanges = JObject.Parse(dbRequest.ProposedChanges);

                        if (dbRequest.ProposedPerson.AzureUniqueId != null)
                            newInstance.SetPropertyValue<ApiPositionInstanceV2>(i => i.AssignedPerson, new ApiPersonV2() { AzureUniqueId = dbRequest.ProposedPerson.AzureUniqueId });

                        if (proposedChanges.TryGetValue("obs", StringComparison.InvariantCultureIgnoreCase, out var obs))
                            newInstance.SetPropertyValue<ApiPositionInstanceV2>(i => i.Obs, obs);

                        if (proposedChanges.TryGetValue("workload", StringComparison.InvariantCultureIgnoreCase, out var workload))
                            newInstance.SetPropertyValue<ApiPositionInstanceV2>(i => i.Workload!, workload);

                        // Should applies from be allowed to be updated on a currently active position?
                        //if (proposedChanges.TryGetValue("appliesFrom", StringComparison.InvariantCultureIgnoreCase, out var appliesFrom))
                        //    newInstance.SetPropertyValue<ApiPositionInstanceV2>(i => i.AppliesFrom, appliesFrom);

                        if (proposedChanges.TryGetValue("appliesTo", StringComparison.InvariantCultureIgnoreCase, out var appliesTo))
                            newInstance.SetPropertyValue<ApiPositionInstanceV2>(i => i.AppliesTo, appliesTo);

                        if (proposedChanges.TryGetValue("location", StringComparison.InvariantCultureIgnoreCase, out var location))
                            newInstance.SetPropertyValue<ApiPositionInstanceV2>(i => i.Location, location.ToObject<ApiPositionLocationV2>()!);

                        // Add the new instance with the changes to the position
                        instances.Add(newInstance);

               
                        var url = $"/projects/{dbRequest.Project.OrgProjectId}/positions/{dbRequest.OrgPositionId}?api-version=2.0";

                        var resp = await client.PutAsync(url, rawPosition);

                        if (!resp.IsSuccessStatusCode)
                            throw new OrgApiError(resp.Response, resp.Content);
                    }
                }
            }

            


        }
    }

    public static class JObjectExtensions
    {
        public static void SetPropertyValue<T>(this JObject jObject, Expression<Func<T, object>> propertySelector, JToken propertyValue)
        {
            var prop = GetPropertyName<T, object>(propertySelector);

            var property = jObject.Properties().FirstOrDefault(p => p.Name.EqualsIgnCase(prop.Name));

            if (property == null)
            {
                var camelCasedPropertyName = CamelCaseProperty(prop.Name);
                jObject.Add(camelCasedPropertyName, propertyValue);
            }
            else
            {
                jObject[property.Name] = propertyValue;
            }
        }

        public static void RemoveProperty(this JObject jObject, params string[] propertyNames)
        {
            foreach (var prop in propertyNames)
            {
                var jProp = jObject.Property(prop, StringComparison.OrdinalIgnoreCase);
                if (jProp is not null)
                    jProp.Remove();
            }
        }

        public static void SetPropertyValue<T>(this JObject jObject, Expression<Func<T, object>> propertySelector, object propertyValue)
        {
            var prop = GetPropertyName<T, object>(propertySelector);

            var property = jObject.Properties().FirstOrDefault(p => p.Name.EqualsIgnCase(prop.Name));

            var tempObject = JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(new { prop = propertyValue }));
            jObject[property!.Name] = tempObject.Property("prop")!.Value;
        }


        public static JArray? GetPropertyCollection<T>(this JObject jObject, Expression<Func<T, object>> propertySelector)
        {
            var selectedPropertyMember = GetPropertyName<T, object>(propertySelector);
            var property = jObject.Properties().FirstOrDefault(p => p.Name.EqualsIgnCase(selectedPropertyMember.Name));

            if (property == null)
            {
                // Does not exist.. Create it
                jObject[selectedPropertyMember.Name] = new JArray();
                return jObject[selectedPropertyMember.Name] as JArray;
            }

            if (property.Value.Type == JTokenType.Null)
                property.Value = new JArray();

            return property.Value as JArray;
        }
        private static PropertyInfo GetPropertyName<T, TValue>(Expression<Func<T, TValue>> selector)
        {
            MemberExpression? body = selector.Body as MemberExpression;

            if (body == null)
            {
                UnaryExpression ubody = (UnaryExpression)selector.Body;
                body = ubody.Operand as MemberExpression;
            }

            return (PropertyInfo)body!.Member;

        }

        public static TValue GetPropertyValue<T, TValue>(this JObject jObject, Expression<Func<T, TValue>> propertySelector)
        {
            var selectedPropertyMember = GetPropertyName<T, TValue>(propertySelector);
            var property = jObject.Properties().FirstOrDefault(p => p.Name.EqualsIgnCase(selectedPropertyMember.Name));

            if (property != null)
            {
                return property.Value.ToObject<TValue>()!;
            }

            return default(TValue)!;
        }

        public static bool EqualsIgnCase(this string source, string query)
        {
            if (source == null)
                return query == null;

            return source.Equals(query, StringComparison.OrdinalIgnoreCase);
        }
        private static string CamelCaseProperty(string propertyName)
        {
            if (propertyName == null)
                throw new ArgumentNullException("Property name cannot be null when converting to camelcase");

            return Char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
        }
    }
}