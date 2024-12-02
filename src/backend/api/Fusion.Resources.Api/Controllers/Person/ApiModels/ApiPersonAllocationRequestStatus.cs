namespace Fusion.Resources.Api.Controllers
{
    public class ApiPersonAllocationRequestStatus
    {
        /// <summary>
        /// Should the request be auto approved?
        /// </summary>
        public bool AutoApproval { get; set; }
        
        /// <summary>
        /// Applicable manager for the user. This is based on who is set as manager in Entra ID.
        /// </summary>
        public ApiPerson? Manager { get; set; }

        /// <summary>
        /// The relevant org unit requests should be assigned to.
        /// </summary>
        public Services.LineOrg.ApiModels.ApiOrgUnit? RequestOrgUnit { get; set; }
    }
}