using Fusion.ApiClients.Org;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Testing.Mocks.OrgService.Api
{
    [ModelBinder(typeof(ProjectResolver))]
    public struct ProjectIdentifier
    {
        public ProjectIdentifier(Guid projectId, bool exists)
        {
            ProjectId = projectId;
            DomainId = null;
            IsUniqueId = true;
            Exists = exists;
        }

        public ProjectIdentifier(string domainId, bool exists)
        {
            ProjectId = null;
            DomainId = domainId;
            IsUniqueId = false;
            Exists = exists;
        }

        public Guid? ProjectId { get; }
        public string DomainId { get; }

        public bool IsUniqueId { get; }
        public bool Exists { get; }

        
    }

    public class ProjectResolver : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.FieldName);
            if (valueProviderResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            //bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);
            var value = valueProviderResult.FirstValue;

            // Check if the argument value is null or empty
            if (string.IsNullOrEmpty(value))
            {
                return Task.CompletedTask;
            }


            // Resolve the project 

            var isUniqueId = Guid.TryParse(value, out Guid uniqueId);

            if (isUniqueId)
            {
                bindingContext.Result = ModelBindingResult.Success(new ProjectIdentifier(uniqueId, OrgServiceMock.projects.Any(p => p.ProjectId == uniqueId)));
            }
            else
            {
                var exists = OrgServiceMock.projects.Any(p => string.Equals(p.DomainId, value, StringComparison.OrdinalIgnoreCase));
                bindingContext.Result = ModelBindingResult.Success(new ProjectIdentifier(value, exists));
            }

            return Task.CompletedTask;
        }
    }

    public static class ProjectIdentifierExtensions
    {
        public static ApiProjectV2 GetProjectOrDefault(this ProjectIdentifier projectIdentifier)
        {
            return projectIdentifier.IsUniqueId switch
            {
                true => OrgServiceMock.projects.FirstOrDefault(p => p.ProjectId == projectIdentifier.ProjectId),
                false => OrgServiceMock.projects.FirstOrDefault(p => string.Equals(p.DomainId, projectIdentifier.DomainId, StringComparison.OrdinalIgnoreCase))
            };
        }
    }
}
