﻿using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Database.Entities;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace Fusion.Resources.Domain.Commands
{
    public class ReplaceContractPersonnel : TrackableRequest<QueryContractPersonnel>
    {
        public ReplaceContractPersonnel(Guid projectId, Guid contractIdentifier, PersonnelId fromPerson, string toUpn, PersonnelId toPerson)
        {
            OrgProjectId = projectId;
            OrgContractId = contractIdentifier;
            FromPerson = fromPerson;
            ToUpn = toUpn;
            ToPerson = toPerson;
        }

        public ReplaceContractPersonnel WithForce(bool force)
        {
            ForceUpdate = force;
            return this;
        }

        public Guid OrgContractId { get; }
        public Guid OrgProjectId { get; }
        public PersonnelId FromPerson { get; }
        public PersonnelId ToPerson { get; }
        public string ToUpn { get; }
        public bool ForceUpdate { get; private set; }


        public class Handler : IRequestHandler<ReplaceContractPersonnel, QueryContractPersonnel>
        {
            private readonly ResourcesDbContext resourcesDb;
            private readonly IMediator mediator;
            private readonly IProfileService profileService;
            private readonly TelemetryClient telemetryClient;

            public Handler(ResourcesDbContext resourcesDb, IMediator mediator, IProfileService profileService, TelemetryClient telemetryClient)
            {
                this.resourcesDb = resourcesDb;
                this.mediator = mediator;
                this.profileService = profileService;
                this.telemetryClient = telemetryClient;
            }

            public async Task<QueryContractPersonnel> Handle(ReplaceContractPersonnel request, CancellationToken cancellationToken)
            {
                var startTime = DateTimeOffset.UtcNow;

                var existingPerson = await resourcesDb.ContractPersonnel
                    .GetById(request.OrgContractId, request.FromPerson)
                    .Include(cp => cp.Person)
                    .FirstOrDefaultAsync();

                var newPerson = await ValidateSubjectAndTargetPersonsAsync(request, existingPerson);

                var existingPersonPreferredContractMail = existingPerson.Person.PreferredContractMail;
                var existingPersonUpn = existingPerson.Person.UPN;

                newPerson.PersonIdReplacements = ApplyPersonIdReplacement(newPerson, existingPerson);

                existingPerson.Person = newPerson;
                existingPerson.Updated = DateTimeOffset.UtcNow;
                existingPerson.UpdatedBy = request.Editor.Person;

                await mediator.Send(new ReplaceProjectPositionInstancesAssignedPerson(request.OrgProjectId, request.OrgContractId, request.FromPerson, request.ToPerson));

                await resourcesDb.SaveChangesAsync();


                if (string.Equals(existingPersonUpn, newPerson.UPN, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(existingPersonPreferredContractMail))
                {
                    // Update newPerson's preferred email in PPL service, since found on existing account matched by UPN
                    var mail = new List<(Guid personnelId, string? preferredMail)> { new(newPerson.Id, existingPersonPreferredContractMail) };
                    await mediator.Send(new UpdateContractPersonnelContactMail(request.OrgContractId, mail));
                }

                await LogReplacementOperationAsync(request, startTime);

                var returnItem = await mediator.Send(new GetContractPersonnelItem(request.OrgContractId, request.ToPerson));
                return returnItem;
            }

            /// <summary>
            /// Ensure we log the replacement operation
            /// </summary>
            /// <param name="request">Metadata about the replacement</param>
            /// <param name="startTime">When the replacement operation started</param>
            /// <returns></returns>
            private async Task LogReplacementOperationAsync(ReplaceContractPersonnel request, DateTimeOffset startTime)
            {
                var props = new Dictionary<string, string>
                {
                    { "UPN", request.ToUpn },
                    { "FromPerson", request.FromPerson.OriginalIdentifier },
                    { "ToPerson", request.ToPerson.OriginalIdentifier },
                    { "OperationDuration", $"{startTime - DateTimeOffset.UtcNow}" },
                    { "Editor", request.Editor.Person.Name }
                };
                var message = $"Replaced contract personnel in contract {request.OrgContractId} on project {request.OrgProjectId}";
                telemetryClient.TrackTrace(message, SeverityLevel.Information, props);

                await mediator.Send(new CreateContractPersonnelReplacementAudit(request.OrgProjectId, request.OrgContractId,
                    message, request.Editor.Person.Name)
                {
                    ChangeType = "Replacement",
                    UPN = request.ToUpn,
                    FromPerson = request.FromPerson.OriginalIdentifier,
                    ToPerson = request.ToPerson.OriginalIdentifier
                });
            }

            /// <summary>
            /// Create history string for replacement operation.
            /// Person may have been replaced before.
            /// </summary>
            /// <param name="newPerson">Replacing person</param>
            /// <param name="existingPerson">Current expired person</param>
            /// <returns></returns>
            private static string? ApplyPersonIdReplacement(DbExternalPersonnelPerson newPerson, DbContractPersonnel existingPerson)
            {
                var existingReplacements = newPerson.PersonIdReplacements?.Split(",").ToList() ?? new List<string>();
                existingReplacements.Add($"{existingPerson.Person.Id}");
                var replacementString = string.Join(",", existingReplacements.Distinct());
                return replacementString;
            }

            /// <summary>
            /// Validate the replacement request. Ensure valid arguments
            /// </summary>
            /// <param name="request">Replacement request</param>
            /// <param name="existingPerson">Expired person to be replaced</param>
            /// <returns></returns>
            private async Task<DbExternalPersonnelPerson> ValidateSubjectAndTargetPersonsAsync(ReplaceContractPersonnel request, DbContractPersonnel? existingPerson)
            {
                try
                {
                    if (string.Equals(request.FromPerson.OriginalIdentifier, request.ToPerson.OriginalIdentifier, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException($"Cannot replace person using personnel identifier '{request.ToPerson.OriginalIdentifier}'. Subject identifier same as target identifer");

                    if (existingPerson?.Person is null)
                        throw new InvalidOperationException($"Cannot locate person using personnel identifier '{request.FromPerson.OriginalIdentifier}'");
                    if (existingPerson.Person.IsDeleted == false)
                        throw new InvalidOperationException($"Cannot replace person using personnel identifier '{request.FromPerson.OriginalIdentifier}' when account is not expired");

                    var newPerson = await profileService.EnsureExternalPersonnelAsync(request.ToUpn, request.ToPerson, existingPerson.Person.FirstName, existingPerson.Person.LastName);

                    if (newPerson is null)
                        throw new InvalidOperationException($"Cannot locate person using personnel identifier '{request.ToPerson.OriginalIdentifier}'");

                    if (existingPerson.Person.Id == newPerson.Id)
                        throw new InvalidOperationException($"Cannot replace person using personnel identifier '{request.FromPerson.OriginalIdentifier}'. New account identified as existing account");

                    if (!request.ForceUpdate)
                        if (!string.Equals(existingPerson.Person.UPN, newPerson.UPN, StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException($"Cannot update person having different UPN. Existing:{existingPerson.Person.UPN}, New:{newPerson.UPN}");

                    return newPerson!;
                }
                catch (InvalidOperationException ioe)
                {
                    telemetryClient.TrackTrace(ioe.Message, SeverityLevel.Error);
                    throw;
                }
            }
        }
    }
}