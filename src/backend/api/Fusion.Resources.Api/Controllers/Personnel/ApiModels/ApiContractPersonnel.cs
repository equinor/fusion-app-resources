using Fusion.Resources.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiContractPersonnel
    {
        public ApiContractPersonnel(QueryContractPersonnel personnel)
        {
            PersonnelId = personnel.PersonnelId;
            AzureUniquePersonId = personnel.AzureUniqueId;
            Name = personnel.Name;
            FirstName = personnel.FirstName;
            LastName = personnel.LastName;
            JobTitle = personnel.JobTitle;
            PhoneNumber = personnel.PhoneNumber;
            Mail = personnel.Mail;
            AzureAdStatus = Enum.Parse<ApiAccountStatus>($"{personnel.AzureAdStatus}", true);
            Disciplines = personnel.Disciplines.Select(d => new ApiPersonnelDiscipline(d)).ToList();
            Created = personnel.Created;
            Updated = personnel.Updated;

            Positions = personnel.Positions?.Select(p => new ApiPositionInstanceReference(p)).ToList();
            Requests = personnel.Requests?.Select(r => new ApiRequestReference(r)).ToList();
        }

        public Guid PersonnelId { get; set; }


        public Guid? AzureUniquePersonId { get; set; }
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string JobTitle { get; set; }
        public string PhoneNumber { get; set; }
        public string Mail { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApiAccountStatus AzureAdStatus { get; set; }

        public bool HasCV { get; set; }

        public List<ApiPersonnelDiscipline> Disciplines { get; set; }

        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? Updated { get; set; }



        public List<ApiPositionInstanceReference>? Positions { get; set; }
        public List<ApiRequestReference>? Requests { get; set; }


        public enum ApiAccountStatus { Available, InviteSent, NoAccount }

        public class ApiRequestReference
        {
            public ApiRequestReference(QueryPersonnelRequestReference request)
            {
                Id = request.Id;
                Created = request.Created;
                Updated = request.Updated;

                State = Enum.Parse<ApiContractPersonnelRequest.ApiRequestState>($"{request.State}", true);


                Position = new ApiRequestPosition(request.Position);
                Project = new ApiProjectReference(request.Project);
                Contract = new ApiContractReference(request.Contract);
            }

            public Guid Id { get; set; }

            public DateTimeOffset Created { get; set; }
            public DateTimeOffset? Updated { get; set; }

            [JsonConverter(typeof(JsonStringEnumConverter))]
            public ApiContractPersonnelRequest.ApiRequestState State { get; set; }

            public ApiRequestPosition Position { get; set; }

            public ApiContractReference Contract { get; set; }
            public ApiProjectReference Project { get; set; }
        }

        public class ApiPositionInstanceReference
        {
            public ApiPositionInstanceReference(QueryOrgPositionInstance instance)
            {
                PositionId = instance.PositionId;
                InstanceId = instance.Id;
                Name = instance.Name;
                Obs = instance.Obs;
                ExternalPositionId = instance.ExternalPositionId;
                AppliesFrom = instance.AppliesFrom;
                AppliesTo = instance.AppliesTo;
                Workload = instance.Workload.GetValueOrDefault(0);

                Project = instance.Project;
                Contract = instance.Contract;
                BasePosition = new ApiBasePosition(instance.BasePosition);
            }


            public Guid PositionId { get; set; }

            /// <summary>
            /// Id of the instance the person is assigned to.
            /// </summary>
            public Guid InstanceId { get; set; }
            public string Name { get; set; }
            public string Obs { get; set; }
            public string ExternalPositionId { get; set; }
            public DateTime? AppliesFrom { get; set; }
            public DateTime? AppliesTo { get; set; }
            public double Workload { get; set; }

            public ApiBasePosition BasePosition { get; set; }

            public Fusion.ApiClients.Org.ApiProjectReferenceV2 Project { get; set; }
            public Fusion.ApiClients.Org.ApiContractReferenceV2 Contract { get; set; }
        }
    }



}