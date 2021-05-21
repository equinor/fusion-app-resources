using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Fusion.Integration;
using Fusion.Integration.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Fusion.Resources.Api.Controllers
{
    public class ProjectResolver : IModelBinder
    {
        private static ConcurrentDictionary<string, ApiProjectIdentifier> resolvedDomains = new ConcurrentDictionary<string, ApiProjectIdentifier>();
        private static ConcurrentDictionary<Guid, ApiProjectIdentifier> resolvedUniqueIds = new ConcurrentDictionary<Guid, ApiProjectIdentifier>();

        public static void ClearCache()
        {
            resolvedDomains.Clear();
            resolvedUniqueIds.Clear();
        }


        public static void ClearCache(Guid orgChartId)
        {
            var domainKeys = resolvedDomains.Where(kv => kv.Value.ProjectId == orgChartId).Select(kv => kv.Key).ToList();
            var uniqueIdKeys = resolvedUniqueIds.Where(kv => kv.Value.ProjectId == orgChartId).Select(kv => kv.Key).ToList();

            domainKeys.ForEach(k => resolvedDomains.TryRemove(k, out _));
            uniqueIdKeys.ForEach(k => resolvedUniqueIds.TryRemove(k, out _));
        }

        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {

            bindingContext.HttpContext.Request.EnableBuffering();

            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.FieldName);
            if (valueProviderResult == ValueProviderResult.None)
            {
                return;
            }

            //bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);
            var value = valueProviderResult.FirstValue;

            // Check if the argument value is null or empty
            if (string.IsNullOrEmpty(value))
            {
                return;
            }


            // Resolve the project 

            var isUniqueId = Guid.TryParse(value, out Guid uniqueId);

            if (!isUniqueId)
            {
                await ResolveDomainIdAsync(bindingContext, value);
            }
            else
            {
                try
                {
                    await ResolveUniqueIdAsync(bindingContext, uniqueId);
                }
                catch (ProjectBinderError)
                {
                    var localFallback = await TryResolveLocallyAsync(bindingContext, uniqueId);
                    if (localFallback == null)
                        throw;
                }
            }




        }

        private async Task ResolveDomainIdAsync(ModelBindingContext bindingContext, string domain)
        {
            var normalizedDomain = domain.ToLower();

            if (resolvedDomains.ContainsKey(normalizedDomain))
            {
                bindingContext.Result = ModelBindingResult.Success(resolvedDomains[domain]);
                return;
            }

            var contextResolver = bindingContext.HttpContext.RequestServices.GetRequiredService<IFusionContextResolver>();

            var results = await contextResolver.QueryContextsAsync(q => q.WhereType(FusionContextType.OrgChart).WhereValue("domainId", domain));

            try
            {
                var orgChart = results.SingleOrDefault();

                if (orgChart is null)
                    throw new ProjectBinderError($"Could not locate any project using domain id '{domain}'");


                if (!Guid.TryParse(orgChart.ExternalId, out Guid orgChartId))
                    throw new ProjectBinderError("Located project context, but the context does not contain a valid org chart id.");


                var dbProject = await GetDbProjectAsync(bindingContext, orgChartId);
                var identifier = new ApiProjectIdentifier(domain, orgChartId, orgChart.Title)
                {
                    LocalEntityId = dbProject?.Id
                };

                resolvedDomains[normalizedDomain] = identifier;

                bindingContext.Result = ModelBindingResult.Success(identifier);
            }
            catch (InvalidOperationException ex)
            {
                var projectNames = results.Select(c => c.Title);
                throw new ProjectBinderError($"Ambigous project identifier. Located multiple projects searching for domain id '{domain}'. Found projects: {string.Join(", ", projectNames)}. {ex.Message}", ex);
            }
        }

        private async Task ResolveUniqueIdAsync(ModelBindingContext bindingContext, Guid uniqueId)
        {

            if (resolvedUniqueIds.ContainsKey(uniqueId))
            {
                bindingContext.Result = ModelBindingResult.Success(resolvedUniqueIds[uniqueId]);
                return;
            }

            var contextResolver = bindingContext.HttpContext.RequestServices.GetRequiredService<IFusionContextResolver>();

            var context = await contextResolver.ResolveContextAsync(uniqueId);

            if (context == null)
                context = await contextResolver.ResolveContextAsync(ContextIdentifier.FromExternalId(uniqueId), FusionContextType.ProjectMaster);              

            if (context == null)
                context = await contextResolver.ResolveContextAsync(ContextIdentifier.FromExternalId(uniqueId), FusionContextType.OrgChart);

            if (context == null)
            {
                throw new ProjectBinderError($"Cannot locate any relevant context for id '{uniqueId}'. Tried context by id, project master and org chart by external id");
            }

            var orgResolver = bindingContext.HttpContext.RequestServices.GetRequiredService<IProjectOrgChartIdResolver>();

            var orgChartId = await orgResolver.ResolveOrgChartIdAsync(ProjectOrganisationIdentifier.FromContextId(context.Id));
            var orgChartContext = await contextResolver.ResolveContextAsync(ContextIdentifier.FromExternalId(orgChartId), FusionContextType.OrgChart);

            var dbProject = await GetDbProjectAsync(bindingContext, uniqueId);
            var identifier = new ApiProjectIdentifier($"{uniqueId}", orgChartId, orgChartContext?.Title ?? string.Empty)
            {
                LocalEntityId = dbProject?.Id
            };

            resolvedUniqueIds[uniqueId] = identifier;
            bindingContext.Result = ModelBindingResult.Success(identifier);
        }

        private async Task<ApiProjectIdentifier?> TryResolveLocallyAsync(ModelBindingContext bindingContext, Guid uniqueId)
        {

            var dbProject = await GetDbProjectAsync(bindingContext, uniqueId);

            if (dbProject == null)
                return null;

            var identifier = new ApiProjectIdentifier($"{uniqueId}", dbProject.OrgProjectId, dbProject.Name)
            {
                LocalEntityId = dbProject.Id
            };

            resolvedUniqueIds[uniqueId] = identifier;
            bindingContext.Result = ModelBindingResult.Success(identifier);


            return identifier;
        }

        private async Task<DbProject?> GetDbProjectAsync(ModelBindingContext bindingContext, Guid orgChartId)
        {
            var db = bindingContext.HttpContext.RequestServices.GetRequiredService<ResourcesDbContext>();

            var dbProject = await db.Projects.FirstOrDefaultAsync(p => p.OrgProjectId == orgChartId);
            return dbProject;
        }
    
    }


}
