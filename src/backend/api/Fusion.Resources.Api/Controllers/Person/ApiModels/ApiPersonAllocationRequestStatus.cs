namespace Fusion.Resources.Api.Controllers
{
    public class ApiPersonAllocationRequestStatus
    {
        public bool AutoApproval { get; set; }
        public ApiPerson? Manager { get; set; }
    }
}