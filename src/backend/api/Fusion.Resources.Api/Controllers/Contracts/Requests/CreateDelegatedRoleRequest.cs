using Newtonsoft.Json;
using System;
using Fusion.AspNetCore.FluentAuthorization;
using Newtonsoft.Json.Converters;
using FluentValidation;

namespace Fusion.Resources.Api.Controllers
{
    public class CreateDelegatedRoleRequest
    {
        public PersonReference Person { get; set; } = null!;

        public DateTimeOffset ValidTo { get; set; } = DateTime.UtcNow.AddYears(1);

        [JsonConverter(typeof(StringEnumConverter))]
        [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
        public ApiDelegatedRoleClassification Classification { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
        public ApiDelegatedRoleType Type { get; set; }

        #region Validator

        public class Validator : AbstractValidator<CreateDelegatedRoleRequest>
        {
            public Validator()
            {
                RuleFor(x => x.Person).BeValidPerson();
                
                RuleFor(x => x.Classification).NotEqual(ApiDelegatedRoleClassification.Unknown)
                    .WithMessage("classification must be specified. Supported values: 'Internal', 'External'.");
                
                RuleFor(x => x.Type).NotEqual(ApiDelegatedRoleType.Unknown)
                    .WithMessage("classification must be specified. Supported values 'CR'");

                RuleFor(x => x.ValidTo).Must(v => v.Date > DateTime.UtcNow.Date && v.Date <= DateTime.UtcNow.AddYears(1).Date)
                    .WithMessage($"Valid to must be a future date, maximum 1 year ahead in time ({DateTime.UtcNow.AddYears(1):yyyy-MM-dd})");
            }
        }

        #endregion
    }
}
