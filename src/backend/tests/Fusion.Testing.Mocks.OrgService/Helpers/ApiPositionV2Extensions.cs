using Fusion.ApiClients.Org;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Testing.Mocks.OrgService
{
    public static class ApiPositionV2Extensions
    {
        public static ApiPositionV2 WithBasePosition(this ApiPositionV2 position, string bpName)
        {
            var bp = PositionBuilder.AllBasePositions.FirstOrDefault(p => StringComparer.OrdinalIgnoreCase.Equals(p.Name, bpName));
            if (bp == null)
                throw new ArgumentException($"Unable to locate base position {bpName} when setting up test position");

            bool hasBpName = position.Name == position.BasePosition.Name;

            position.BasePosition = bp;
            if (hasBpName)
                position.Name = bp.Name;

            return position;
        }

        public static ApiPositionV2 WithRandomBasePosition(this ApiPositionV2 position, string except = null)
        {
            var faker = new Bogus.Faker();

            var bp = faker.PickRandom(PositionBuilder.AllBasePositions);

            while (except != null && bp.Name == except && bp.Name == "Project Director")
            {
                bp = faker.PickRandom(PositionBuilder.AllBasePositions);
            }

            bool hasBpName = position.Name == position.BasePosition.Name;

            position.BasePosition = bp;
            if (hasBpName)
                position.Name = bp.Name;

            return position;
        }

        public static ApiPositionV2 WithInstances(this ApiPositionV2 position, Action<PositionInstanceBuilder> setup)
        {
            var builder = new PositionInstanceBuilder(position);
            setup(builder);

            return position;
        }
        public static ApiPositionV2 WithInstances(this ApiPositionV2 position, IEnumerable<ApiPositionInstanceV2> copyInstances)
        {
            var copiedInstances = copyInstances.Select(i => i.Clone()).ToList();

            // Reset ids.
            copiedInstances.ForEach(i => i.Id = Guid.Empty);

            position.Instances = copiedInstances;

            return position;
        }

        public static ApiPositionV2 WithInstances(this ApiPositionV2 position, int count)
        {
            var instances = PositionInstanceBuilder.CreateInstanceStack(5, count);
            position.Instances = instances.Take(count).ToList();
            return position;
        }

        /// <summary>
        /// Assigns the person to all instances
        /// </summary>
        public static ApiPositionV2 WithAssignedPerson(this ApiPositionV2 position, Integration.Profile.ApiClient.ApiPersonProfileV3 person) => WithAssignedPerson(position, person.Mail);
        public static ApiPositionV2 WithAssignedPerson(this ApiPositionV2 position, string personMail)
        {
            position.Instances.ForEach(i => i.AssignedPerson = new ApiPersonV2 { Mail = personMail });
            return position;
        }

        public static ApiPositionV2 WithEnsuredFutureInstances(this ApiPositionV2 position)
        {
            // Already an instance with applies to past today
            if (position.Instances.Any(i => i.AppliesTo > DateTime.UtcNow))
                return position;

            // Pick last instance and extend..
            var lastInstance = position.Instances.OrderBy(i => i.AppliesTo).Last();
            lastInstance.AppliesTo = DateTime.UtcNow.AddDays(10);

            return position;
        }

        /// <summary>
        /// Sets the parent position for all instances.
        /// </summary>
        public static ApiPositionV2 WithParentPosition(this ApiPositionV2 position, Guid? parentPoisitionId)
        {
            position.Instances.ForEach(i => i.ParentPositionId = parentPoisitionId);
            return position;
        }

        public static ApiPositionV2 WithTaskOwner(this ApiPositionV2 position, Guid parentPoisitionId)
        {
            position.Instances.ForEach(i => i.AddTaskOwner(parentPoisitionId));
            return position;
        }
    }

}
