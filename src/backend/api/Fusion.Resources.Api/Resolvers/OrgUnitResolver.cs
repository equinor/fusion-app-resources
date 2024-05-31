using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Fusion.Resources.Domain;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace Fusion.Resources.Api.Controllers
{
    public class OrgUnitResolver : IModelBinder
    {
        private static ConcurrentDictionary<string, OrgUnitIdentifier> resolvedIdentifier = new ConcurrentDictionary<string, OrgUnitIdentifier>();

        public static void ClearCache()
        {
            resolvedIdentifier.Clear();
        }


        public static void ClearCache(string identifier)
        {
            var cachedItems = resolvedIdentifier
                .Where(kv => kv.Value.SapId.EqualsIgnCase(identifier) || kv.Value.FullDepartment.EqualsIgnCase(identifier))
                .Select(kv => kv.Key)
                .ToList();

            cachedItems.ForEach(k => resolvedIdentifier.TryRemove(k, out _));
        }

        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {

            bindingContext.HttpContext.Request.EnableBuffering();

            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.FieldName);
            if (valueProviderResult == ValueProviderResult.None)
            {
                return;
            }

            var value = valueProviderResult.FirstValue;


            // Resolve the project 

            var identifier = await ResolveOrgUnitAsync(bindingContext, value);

            // We want to return an identifier so action can handle not found.
            bindingContext.Result = ModelBindingResult.Success(identifier);
        }

        private async Task<OrgUnitIdentifier> ResolveOrgUnitAsync(ModelBindingContext bindingContext, string? identifier)
        {
            // Check if the argument value is null or empty
            if (string.IsNullOrEmpty(identifier))
            {
                return OrgUnitIdentifier.NotFound(identifier ?? string.Empty);
            }

            var normalizedDomain = identifier.ToLower();

            if (resolvedIdentifier.ContainsKey(normalizedDomain))
            {
                return resolvedIdentifier[identifier];
            }

            var mediator = bindingContext.HttpContext.RequestServices.GetRequiredService<IMediator>();

            var result = await mediator.Send(new ResolveLineOrgUnit(identifier));

            var resolvedOrgUnitIdentifier = result is null ? OrgUnitIdentifier.NotFound(identifier) 
                : new OrgUnitIdentifier(identifier, result.SapId, result.FullDepartment, result.Name);

            resolvedIdentifier[identifier] = resolvedOrgUnitIdentifier;

            return resolvedOrgUnitIdentifier;
        }
    }
}
