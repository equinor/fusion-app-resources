using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
using System;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiDelegatedRole
    {
        public ApiDelegatedRole(QueryDelegatedRole role)
        {
            Classification = role.Classification switch
            {
                DbDelegatedRoleClassification.External => ApiDelegatedRoleClassification.External,
                DbDelegatedRoleClassification.Internal => ApiDelegatedRoleClassification.Internal,
                _ => ApiDelegatedRoleClassification.Unknown
            };

            Type = role.Type switch
            {
                DbDelegatedRoleType.CR => ApiDelegatedRoleType.CR,
                _ => ApiDelegatedRoleType.Unknown
            };

            Id = role.Id;

            Created = role.Created;
            ValidTo = role.ValidTo;
            RecertifiedDate = role.RecertifiedDate;

            Person = new ApiPerson(role.Person);
            CreatedBy = new ApiPerson(role.CreatedBy);

            if (role.RecertifiedBy != null)
                RecertifiedBy = new ApiPerson(role.RecertifiedBy);

            Project = new ApiProjectReference(role.Project);
            Contract = new ApiContractReference(role.Contract);
        }

        public Guid Id { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApiDelegatedRoleClassification Classification { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApiDelegatedRoleType Type { get; set; }

        public DateTimeOffset Created { get; set; }
        public DateTimeOffset ValidTo { get; set; }
        public DateTimeOffset? RecertifiedDate { get; set; }

        public ApiPerson Person { get; set; }
        public ApiPerson CreatedBy { get; set; }
        public ApiPerson? RecertifiedBy { get; set; }

        public ApiProjectReference Project { get; set; }
        public ApiContractReference Contract { get; set; }
    }
}
