using System.Threading.Tasks;
using Fusion.Resources.Functions.Common.ApiClients;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Fusion.Summary.Functions.Functions;

// TODO: For testing, will be renamed/removed
public class TestFunctions
{
    private readonly ILineOrgApiClient lineOrgApiClient;

    public TestFunctions(ILineOrgApiClient lineOrgApiClient)
    {
        this.lineOrgApiClient = lineOrgApiClient;
    }

    [FunctionName("department-resource-owner-sync")]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Admin, "get")] HttpRequest req)
    {
        return new OkObjectResult($"Welcome to Azure Functions, {req.Query["name"]}!");
    }

    [FunctionName("get-lineorg")]
    public async Task<IActionResult> Run2(
        [HttpTrigger(AuthorizationLevel.Admin, "get")] HttpRequest req)
    {
        return new OkObjectResult(await lineOrgApiClient.GetOrgUnitDepartmentsAsync());
    }
}