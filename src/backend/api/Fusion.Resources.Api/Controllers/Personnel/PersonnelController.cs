using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Bogus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Logging;

namespace Fusion.Resources.Api.Controllers
{

    [Authorize]
    [ApiController]
    public class PersonnelController : ControllerBase
    {
        
        [HttpGet("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/personnel")]
        public async Task<ActionResult<ApiCollection<ApiContractPersonnel>>> GetContractPersonnel([FromRoute]ProjectIdentifier projectIdentifier, string contractIdentifier) 
        {

            var personnel = new Faker<ApiContractPersonnel>()
                .RuleFor(p => p.PersonnelId, f => Guid.NewGuid())
                .RuleFor(p => p.AzureUniquePersonId, f => f.PickRandom<Guid?>(new[] { (Guid?)null, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() }))
                .RuleFor(p => p.Name, f => f.Person.FullName)
                .RuleFor(p => p.Mail, f => f.Person.Email)
                .RuleFor(p => p.JobTitle, f => f.Name.JobTitle())
                .RuleFor(p => p.PhoneNumber, f => f.Person.Phone)
                .RuleFor(p => p.HasCV, f => f.Random.Bool())
                .RuleFor(p => p.AzureAdStatus, f => f.PickRandomWithout<ApiContractPersonnel.ApiAccountStatus>(ApiContractPersonnel.ApiAccountStatus.NoAccount))
                .FinishWith((f, p) =>
                {
                    if (p.AzureUniquePersonId == null)
                    {
                        p.AzureAdStatus = ApiContractPersonnel.ApiAccountStatus.NoAccount;
                    }

                    p.Disciplines = Enumerable.Range(0, f.Random.Number(1, 4)).Select(i => new ApiPersonnelDiscipline { Name = f.Hacker.Adjective() }).ToList();
                })
                .Generate(new Random().Next(50, 200));

            var collection = new ApiCollection<ApiContractPersonnel>(personnel);
            return collection;
        }

        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/personnel")]
        public async Task<ActionResult<ApiContractPersonnel>> CreateContractPersonnel(string projectIdentifier, string contractIdentifier, [FromBody] CreateContractPersonnelRequest request)
        {

            var person = new Faker<ApiContractPersonnel>()
                .RuleFor(p => p.PersonnelId, f => Guid.NewGuid())
                .RuleFor(p => p.AzureUniquePersonId, f => f.PickRandom<Guid?>(new[] { (Guid?)null, Guid.NewGuid() }))
                .RuleFor(p => p.AzureAdStatus, f => f.PickRandom<ApiContractPersonnel.ApiAccountStatus>())
                .FinishWith((f, p) => p.AzureAdStatus = p.AzureUniquePersonId == null ? ApiContractPersonnel.ApiAccountStatus.NoAccount : p.AzureAdStatus)
                .Generate();


            var item = new ApiContractPersonnel()
            {
                AzureAdStatus = person.AzureAdStatus,
                AzureUniquePersonId = person.AzureUniquePersonId,
                Name = request.Name,
                JobTitle = request.JobTitle,
                Mail = request.Mail,
                PhoneNumber = request.PhoneNumber
            };
            
            return Created($"/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/personnel/{item.Mail}", item);
        }

        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/personnel-collection")]
        public async Task<ActionResult<ApiBatchResponse<ApiContractPersonnel>>> CreateContractPersonnelBatch(string projectIdentifier, string contractIdentifier, [FromBody] IEnumerable<CreateContractPersonnelRequest> requests)
        {

            var itemsToCreate = requests.ToList();


            var persons = new Faker<ApiContractPersonnel>()
                .RuleFor(p => p.AzureUniquePersonId, f => f.PickRandom<Guid?>(new[] { (Guid?)null, Guid.NewGuid() }))
                .RuleFor(p => p.AzureAdStatus, f => f.PickRandom<ApiContractPersonnel.ApiAccountStatus>())
                .FinishWith((f, p) => p.AzureAdStatus = p.AzureUniquePersonId == null ? ApiContractPersonnel.ApiAccountStatus.NoAccount : p.AzureAdStatus)
                .Generate(itemsToCreate.Count);


            var createdItems = itemsToCreate.Select((i, idx) => new ApiContractPersonnel()
            {
                AzureAdStatus = persons[idx].AzureAdStatus,
                AzureUniquePersonId = persons[idx].AzureUniquePersonId,
                Name = i.Name,
                JobTitle = i.JobTitle,
                Mail = i.Mail,
                PhoneNumber = i.PhoneNumber
            });

            var returnItems = createdItems.Select(i => new ApiBatchItemResponse<ApiContractPersonnel>(i, HttpStatusCode.Created)).ToList();

            // Introduce randomness
            if (itemsToCreate.Count >= 5)
            {
                var faker = new Faker();            
                var invalidItem = faker.PickRandom(returnItems);
                invalidItem.Code = HttpStatusCode.Conflict;
                invalidItem.Message = "The person has already been added to the personnel collection for the contract";

                var errorItem = faker.PickRandom(returnItems.Except(new[] { invalidItem }));
                invalidItem.Code = HttpStatusCode.InternalServerError;
                invalidItem.Message = "Unexpected error occured while trying to add the person";

                var illegalItem = faker.PickRandom(returnItems.Except(new[] { invalidItem, errorItem }));
                invalidItem.Code = HttpStatusCode.BadRequest;
                invalidItem.Message = "The specified person cannot be added to ";
            }

            return new ApiBatchResponse<ApiContractPersonnel>(returnItems);
        }

        [HttpDelete("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/personnel/{personIdentifier}")]
        public async Task<ActionResult> DeleteContractPersonnel(string projectIdentifier, string contractIdentifier, string personIdentifier)
        {
            return NoContent();
        }
    
    }


}
