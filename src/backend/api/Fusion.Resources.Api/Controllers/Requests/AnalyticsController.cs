using System;
using System.Linq;
using System.Text.Json.Serialization;
using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Queries;


namespace Fusion.Resources.Api.Controllers
{
    [ApiVersion("1.0-preview")]
    [ApiVersion("1.0")]
    [Authorize]
    [ApiController]
    public class AnalyticsController : ResourceControllerBase
    {

        [HttpGet("/analytics/requests/internal")]
        public async Task<ActionResult<ApiCollection<ApiResourceAllocationRequestForAnalytics>>> GetAllRequests([FromQuery] ODataQueryParams query)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    or.GlobalRoleAccess("Fusion.Analytics.Requests");
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var requestQuery = await DispatchAsync(new GetResourceAllocationRequestsForAnalytics(query));
            var apiModel = requestQuery.Select(ApiResourceAllocationRequestForAnalytics.ForAnalytics).ToList();
            var collection = new ApiCollection<ApiResourceAllocationRequestForAnalytics>(apiModel) { TotalCount = requestQuery.TotalCount };
            return collection;
        }

        [HttpGet("/analytics/absence/internal")]
        public async Task<ActionResult<ApiCollection<ApiPersonAbsenceForAnalytics>>> GetPersonsAbsence([FromQuery] ODataQueryParams query)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    or.GlobalRoleAccess("Fusion.Analytics.Requests");
                });

            });
            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var allAbsenceQuery = await DispatchAsync(new GetPersonsAbsenceForAnalytics(query));
            var apiModel = allAbsenceQuery.Select(ApiPersonAbsenceForAnalytics.CreateWithoutConfidentialTaskInfoForAnalytics);

            var collection = new ApiCollection<ApiPersonAbsenceForAnalytics>(apiModel) { TotalCount = allAbsenceQuery.TotalCount };
            return collection;
        }

        #region Analytics Api Models
        public class ApiPersonAbsenceForAnalytics
        {

            private ApiPersonAbsenceForAnalytics(QueryPersonAbsenceBasic absence)
            {
                Id = absence.Id;
                AppliesFrom = absence.AppliesFrom;
                AppliesTo = absence.AppliesTo;
                Type = Enum.Parse<ApiPersonAbsence.ApiAbsenceType>($"{absence.Type}", true);
                AbsencePercentage = absence.AbsencePercentage;

                IsPrivate = absence.IsPrivate;
                Comment = absence.Comment;
                TaskDetails = (absence.TaskDetails != null) ? new ApiTaskDetails(absence.TaskDetails) : null;

                if (absence.Person is not null) Person = new ApiPerson(absence.Person);

                if (absence.IsPrivate || absence.Type == QueryAbsenceType.Absence)
                {
                    Comment = "Not disclosed.";
                    TaskDetails = (absence.TaskDetails != null) ? ApiTaskDetails.Hidden : null;
                }
            }

            public static ApiPersonAbsenceForAnalytics CreateWithoutConfidentialTaskInfoForAnalytics(QueryPersonAbsenceBasic absence) => new(absence);

            public Guid Id { get; set; }
            public bool IsPrivate { get; set; }
            public string? Comment { get; set; }
            public ApiPerson? Person { get; set; }
            public ApiTaskDetails? TaskDetails { get; set; }
            public DateTimeOffset AppliesFrom { get; set; }
            public DateTimeOffset? AppliesTo { get; set; }
            public ApiPersonAbsence.ApiAbsenceType Type { get; set; }
            public double? AbsencePercentage { get; set; }
        }
        public class ApiResourceAllocationRequestForAnalytics
        {
            private ApiResourceAllocationRequestForAnalytics(QueryResourceAllocationRequest query)
            {
                Id = query.RequestId;
                Number = query.RequestNumber;

                AssignedDepartment = query.AssignedDepartment;
                if (query.AssignedDepartmentDetails is not null)
                    AssignedDepartmentDetails = new ApiDepartment(query.AssignedDepartmentDetails);

                Discipline = query.Discipline;
                State = query.State;
                Type = $"{query.Type}";
                SubType = query.SubType;

                if (query.ProposedPerson != null)
                {
                    ProposedPersonAzureUniqueId = query.ProposedPerson.AzureUniqueId;
                }

                Project = new ApiProjectReference(query.Project);

                OrgPositionId = query.OrgPositionId;
                OrgPositionInstanceId = query.OrgPositionInstanceId;

                AdditionalNote = query.AdditionalNote;

                ProposedChanges = new ApiPropertiesCollection(query.ProposedChanges);
                ProposalParameters = new ApiProposalParameters(query.ProposalParameters);

                Created = query.Created;
                Updated = query.Updated;
                CreatedBy = new ApiPerson(query.CreatedBy);
                UpdatedBy = ApiPerson.FromEntityOrDefault(query.UpdatedBy);

                LastActivity = query.LastActivity;
                IsDraft = query.IsDraft;

                ProvisioningStatus = new ApiProvisioningStatus(query.ProvisioningStatus);
            }

            public static ApiResourceAllocationRequestForAnalytics ForAnalytics(QueryResourceAllocationRequest query) => new(query);
            public Guid Id { get; set; }
            public long Number { get; set; }

            public string? AssignedDepartment { get; set; }
            public ApiDepartment? AssignedDepartmentDetails { get; }
            public string? Discipline { get; set; }
            public string? State { get; set; }
            public string Type { get; set; }
            public string? SubType { get; set; }
            public ApiProjectReference Project { get; set; }
            public Guid? OrgPositionId { get; set; }
            public Guid? OrgPositionInstanceId { get; set; }
            public string? AdditionalNote { get; set; }

            public ApiPropertiesCollection? ProposedChanges { get; set; }
            public Guid? ProposedPersonAzureUniqueId { get; set; }
            public ApiProposalParameters? ProposalParameters { get; set; }


            public DateTimeOffset Created { get; set; }
            public ApiPerson CreatedBy { get; set; }

            public DateTimeOffset? Updated { get; set; }
            public ApiPerson? UpdatedBy { get; set; }

            public DateTimeOffset? LastActivity { get; set; }
            public bool IsDraft { get; set; }
            public ApiProvisioningStatus ProvisioningStatus { get; set; }

        }
        #endregion
    }
}
