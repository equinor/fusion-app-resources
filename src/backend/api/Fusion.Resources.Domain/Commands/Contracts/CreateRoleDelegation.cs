﻿using FluentValidation;
using Fusion.Integration.Profile;
using Fusion.Integration.Roles;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class CreateRoleDelegation : TrackableRequest<QueryDelegatedRole>
    {
        public CreateRoleDelegation(Guid orgProjectId, Guid orgContractId)
        {
            OrgProjectId = orgProjectId;
            OrgContractId = orgContractId;
        }

        public Guid OrgProjectId { get; }
        public Guid OrgContractId { get; }
        public PersonId Person { get; private set; } = null!;
        public bool? IsInternal { get; private set; }
        public DateTimeOffset ValidTo { get; private set; } = DateTimeOffset.UtcNow.AddYears(1);

        public DbDelegatedRoleType Type { get; private set; }

        public CreateRoleDelegation ForPerson(PersonId person)
        {
            Person = person;
            return this;
        }

        public CreateRoleDelegation SetIsInternal(bool isInternal)
        {
            IsInternal = isInternal;
            return this;
        }

        public CreateRoleDelegation ValidToDate(DateTimeOffset validTo)
        {
            ValidTo = validTo;
            return this;
        }

        public CreateRoleDelegation SetType(DbDelegatedRoleType type)
        {
            Type = type;
            return this;
        }

        public class Validator : AbstractValidator<CreateRoleDelegation>
        {
            public Validator(ResourcesDbContext dbContext)
            {
                RuleFor(x => x.Person).NotNull();
                RuleFor(x => x.ValidTo).Must(v => v.UtcDateTime.Date <= DateTime.UtcNow.AddYears(1).Date)
                    .WithMessage("Valid to cannot exceed 1 year");
                RuleFor(x => x.IsInternal).NotNull();
                RuleFor(x => x.OrgProjectId).MustAsync(async (id, cancel) =>
                {
                    return await dbContext.Projects.AnyAsync(p => p.OrgProjectId == id);
                }).WithMessage("Project with org id must exist in database");

                RuleFor(x => x.OrgContractId).MustAsync(async (id, cancel) =>
                {
                    return await dbContext.Contracts.AnyAsync(p => p.OrgContractId == id);
                }).WithMessage("Contract must be allocated");
            }
        }

        public class Handler : IRequestHandler<CreateRoleDelegation, QueryDelegatedRole>
        {
            private readonly ResourcesDbContext dbContext;
            private readonly IProfileService profileService;
            private readonly IMediator mediator;

            public Handler(ResourcesDbContext dbContext, IProfileService profileService, IMediator mediator)
            {
                this.dbContext = dbContext;
                this.profileService = profileService;
                this.mediator = mediator;
            }

            public async Task<QueryDelegatedRole> Handle(CreateRoleDelegation request, CancellationToken cancellationToken)
            {

                var contract = await dbContext.Contracts.FirstAsync(c => c.OrgContractId == request.OrgContractId);
                var project = await dbContext.Projects.FirstAsync(c => c.OrgProjectId == request.OrgProjectId);
                var person = await profileService.EnsurePersonAsync(request.Person);

                if (person == null)
                    throw new InvalidOperationException($"Person could not be resolved with identifier '{request.Person.OriginalIdentifier}'");

                var classification = request.IsInternal.GetValueOrDefault(false) ? DbDelegatedRoleClassification.Internal : DbDelegatedRoleClassification.External;

                await ValidateAsync(request, person, classification);

                var role = new DbDelegatedRole
                {
                    ProjectId = project.Id,
                    ContractId = contract.Id,
                    Type = request.Type,
                    PersonId = person.Id,
                    Classification = classification,
                    Created = DateTimeOffset.UtcNow,
                    CreatedById = request.Editor.Person.Id,
                    ValidTo = request.ValidTo
                };

                await dbContext.AddAsync(role);
                await dbContext.SaveChangesAsync();

                await mediator.Send(new CreateContractReadRoleAssignment(role.Id));
                await mediator.Publish(new Notifications.ContractRoleDelegated(role.Id));

                return new QueryDelegatedRole(role);
            }

            private async Task<ValueTask> ValidateAsync(CreateRoleDelegation request, DbPerson person, DbDelegatedRoleClassification classification)
            {
                // Check that the person does not already have an exact match of the delegate role.
                if (await dbContext.DelegatedRoles.AnyAsync(r => r.Type == request.Type 
                    && r.Contract.OrgContractId == request.OrgContractId 
                    && r.PersonId == person.Id
                    && r.Classification == classification))
                    throw new RoleDelegationExistsError();


                // Check that an internal role is not granted to an external account
                if (classification == DbDelegatedRoleClassification.Internal)
                {
                    if (string.Equals(person.AccountType, $"{FusionAccountType.External}", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Cannot delegate internal roles to external accounts");
                    }
                }

                return new ValueTask();
            }
        }
    }

}
