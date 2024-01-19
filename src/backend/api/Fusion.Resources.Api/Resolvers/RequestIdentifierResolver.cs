using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Fusion.Resources.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Fusion.Resources.Api.Controllers
{
    public class RequestIdentifierResolver : IModelBinder
    {
        private static ConcurrentDictionary<string, RequestIdentifier> resolvedIdentifiers = new ConcurrentDictionary<string, RequestIdentifier>();

        public static void ClearCache()
        {
            resolvedIdentifiers.Clear();
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

            
            if (resolvedIdentifiers.TryGetValue(value.ToLower(), out RequestIdentifier? identifier))
            {
                bindingContext.Result = ModelBindingResult.Success(identifier);
            }
            else
            {
                identifier = await ResolveRequestAsync(bindingContext, value);
            
                // Add to cache
                resolvedIdentifiers.TryAdd(value.ToLower(), identifier);
            }

            bindingContext.Result = ModelBindingResult.Success(identifier);
        }

        private async Task<RequestIdentifier> ResolveRequestAsync(ModelBindingContext bindingContext, string identifier)
        {
            var dbContext = bindingContext.HttpContext.RequestServices.GetRequiredService<ResourcesDbContext>();

            if (Guid.TryParse(identifier, out Guid uniqueId))
            {
                var request = await dbContext.ResourceAllocationRequests
                    .Where(r => r.Id == uniqueId)
                    .Select(r => new { r.Id, r.RequestNumber })
                    .FirstOrDefaultAsync();
                if (request is not null)
                    return new RequestIdentifier(identifier, request.Id, request.RequestNumber);
            }
            else if (long.TryParse(identifier, out long requestNumber))
            {
                var request = await dbContext.ResourceAllocationRequests
                    .Where(r => r.RequestNumber == requestNumber)
                    .Select(r => new { r.Id, r.RequestNumber })
                    .FirstOrDefaultAsync();
                if (request is not null)
                    return new RequestIdentifier(identifier, request.Id, request.RequestNumber);
            }
            else
            {
                // For now, just let invalid values return 
                bindingContext.Result = ModelBindingResult.Failed();
            }

            // Not found
            return RequestIdentifier.NotFound(identifier);
        }
    }

}
