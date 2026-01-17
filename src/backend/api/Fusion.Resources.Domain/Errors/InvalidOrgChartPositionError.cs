using System;
using Fusion.Services.Org.ApiModels;

#nullable enable

namespace Fusion.Resources
{
    public class InvalidOrgChartPositionError : Exception
    {
        private InvalidOrgChartPositionError(InvalidType type, string message, Guid positionId) : base(message)
        {
            Type = type;
            PositionId = positionId;
        }

        public InvalidType Type { get; }
        public Guid PositionId { get; }

        public ApiPositionV2? Position { get; private set; }


        public static InvalidOrgChartPositionError NotFound(Guid positionId)
        {
            return new InvalidOrgChartPositionError(InvalidType.NotFound, $"Could not locate position with id '{positionId}'", positionId);
        }

        public static InvalidOrgChartPositionError InvalidProject(ApiPositionV2 position)
        {
            return new InvalidOrgChartPositionError(InvalidType.Context, $"Position does not belong to the correct project. It belongs to {position.Project.Name}", position.Id)
            {
                Position = position
            };
        }

        public static InvalidOrgChartPositionError InvalidContract(ApiPositionV2 position)
        {
            return new InvalidOrgChartPositionError(InvalidType.Context, $"Position does not belong to the correct contract. It belongs to {position.Contract.Name} ({position.Contract.ContractNumber})", position.Id)
            {
                Position = position
            };
        }

        public enum InvalidType { NotFound, Context }
    }
}
