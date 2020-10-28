using Fusion.ApiClients.Org;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Testing.Mocks.OrgService
{
    public class PositionInstanceBuilder
        {
            private readonly ApiPositionV2 position;

            public PositionInstanceBuilder(ApiPositionV2 position)
            {
                this.position = position;

                // Clear instance auto generated
                this.position.Instances = new List<ApiPositionInstanceV2>();
            }

            public ApiPositionInstanceV2 AddInstance(DateTime from, TimeSpan duration, Guid? parentPositionId = null)
            {
                var instance = CreateInstance().Generate();
                instance.AppliesFrom = from;
                instance.AppliesTo = from.Add(duration);
                instance.ParentPositionId = parentPositionId;

                position.Instances.Add(instance);

                return instance;
            }

            public ApiPositionInstanceV2 AddInstance(TimeSpan duration, Guid? parentPositionId = null)
            {
                var lastInstance = position.Instances.OrderBy(i => i.AppliesTo).LastOrDefault();

                var startDate = lastInstance?.AppliesTo.AddDays(1);

                if (startDate == null)
                {
                    startDate = new Bogus.Faker().Date.Past(2);
                }

                var instance = CreateInstance().Generate();
                instance.AppliesFrom = startDate.Value;
                instance.AppliesTo = startDate.Value.Add(duration);
                instance.ParentPositionId = parentPositionId;

                position.Instances.Add(instance);

                return instance;
            }

        public static List<ApiPositionInstanceV2> CreateInstanceStack(int maxCount = 4, int minCount = 1) => CreateInstanceStack(new Bogus.Faker(), maxCount, minCount);
        public static List<ApiPositionInstanceV2> CreateInstanceStack(Bogus.Faker rangeFaker, int maxCount = 4, int minCount = 1)
        {
            var start = rangeFaker.Date.Past(2).Date;
            var end = rangeFaker.Date.Future(2).Date;
            var count = rangeFaker.Random.Int(minCount, maxCount);

            var faker = new Bogus.Faker<ApiPositionInstanceV2>()
                .RuleFor(i => i.Id, f => Guid.NewGuid())
                .RuleFor(i => i.ExternalId, f => f.Random.AlphaNumeric(3))
                .RuleFor(i => i.Calendar, f => "Normal")
                .RuleFor(i => i.Workload, f => f.Random.Double(0, 100));

            var instances = faker.Generate(count);

            // Space out instances
            foreach (var instance in instances)
            {
                var newInstanceEnd = end;
                if (newInstanceEnd <= start)
                    newInstanceEnd = start.AddDays(60);

                var instanceEnd = rangeFaker.Date.Between(start.AddDays(30), newInstanceEnd);
                instance.AppliesFrom = start;
                instance.AppliesTo = instanceEnd;

                start = instanceEnd.Date.AddDays(1);
            }

            return instances;
        }

        public static Bogus.Faker<ApiPositionInstanceV2> CreateInstance()
        {
            return new Bogus.Faker<ApiPositionInstanceV2>()
                .RuleFor(i => i.Id, f => Guid.NewGuid())
                .RuleFor(i => i.ExternalId, f => f.Random.AlphaNumeric(3))
                .RuleFor(i => i.Calendar, f => "Normal")
                .RuleFor(i => i.Workload, f => f.Random.Double(0, 100));
        }



    }

}
