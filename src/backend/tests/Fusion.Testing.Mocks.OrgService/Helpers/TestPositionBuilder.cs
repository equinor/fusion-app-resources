using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Fusion.Services.Org.ApiModels;

namespace Fusion.Testing.Mocks.OrgService
{
    public static class PositionBuilder
    {
        public static readonly SemaphoreSlim semaphoreBp = new SemaphoreSlim(1);
        public static ApiPositionV2 NewPosition()
        {
            return CreateTestPosition()
                .Generate();
        }

        public static Bogus.Faker<ApiPositionV2> CreateTestPosition()
        {
            return new Bogus.Faker<ApiPositionV2>()
                .RuleFor(p => p.Id, f => Guid.NewGuid())
                .RuleFor(p => p.Properties, f => new Dictionary<string, object>())
                .RuleFor(p => p.ExternalId, f => f.Random.AlphaNumeric(5))
                .RuleFor(p => p.BasePosition, f => f.PickRandom(AllBasePositions.Except(new[] { DirectorBasePosition })))
                .RuleFor(p => p.Instances, f => PositionInstanceBuilder.CreateInstanceStack(f, 4))
                .FinishWith((f, p) =>
                {
                    p.Name = p.BasePosition.Name;
                    p.Instances.ForEach(i => i.PositionId = p.Id);
                });
        }


        private static Lazy<List<ApiBasePositionV2>> basePositionCache = new Lazy<List<ApiBasePositionV2>>(() =>
        {
            var bps = GetBasePositionCSVImport();
            return bps.Select(bp => new ApiBasePositionV2
            {
                Id = bp.Id,
                Department = bp.Department,
                Discipline = bp.Discipline,
                Name = bp.Name,
                Inactive = bp.Inactive,
                ProjectType = bp.ProjectType
            }).ToList();
        }, isThreadSafe: true);
        public static IEnumerable<ApiBasePositionV2> AllBasePositions
        {
            get
            {
                semaphoreBp.Wait();
                try
                {
                    return basePositionCache.Value.ToArray();
                }
                finally
                {
                    semaphoreBp.Release();
                }
            }
        }

        public static ApiBasePositionV2 DirectorBasePosition => AllBasePositions.First(bp => bp.Id == new Guid("f942a973-9cac-4a96-8bc7-a9e41141f021"));

        private static List<ApiBasePositionV2> GetBasePositionCSVImport()
        {
            var resourceData = AssemblyUtils.GetResourceFromCurrentAssembly($@"Fusion.Testing.Mocks.OrgService.Data.BasePositions.csv");
            var data = resourceData.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            List<ApiBasePositionV2> positions = data.Skip(1)
                .Where(l => !string.IsNullOrEmpty(l))   // Trim any empty lines
                .Select(DeserializeCSVBasePosition)
                .ToList();

            return positions;
        }

        private static ApiBasePositionV2 DeserializeCSVBasePosition(string line)
        {
            string[] tokens = line.Split(';');  // GUID, Title, Discipline, Department

            ApiBasePositionV2 p = new ApiBasePositionV2
            {
                Id = new Guid(tokens[0]),
                Name = tokens[1].Trim(),
                ProjectType = tokens[2].Trim(),
                Discipline = (tokens[3] ?? "").Trim(),
                Department = (tokens[4] ?? "").Trim(),
                Inactive = bool.Parse(tokens[5])
            };

            return p;
        }

        static class AssemblyUtils
        {
            public static string GetResourceFromCurrentAssembly(string resourcePath)
            {
                return GetResource(Assembly.GetCallingAssembly(), resourcePath);
            }

            public static string GetResource(Assembly assembly, string resourcePath)
            {
                string[] manifestResourceNames = assembly.GetManifestResourceNames();
                if (!manifestResourceNames.Contains(resourcePath))
                {
                    throw new ArgumentNullException("Could not locate resource in assembly. Located: " + string.Join(",", manifestResourceNames));
                }
                using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
                {
                    using (StreamReader streamReader = new StreamReader(stream))
                    {
                        return streamReader.ReadToEnd();
                    }
                }
            }
        }

        public static void AddBaseposition(ApiBasePositionV2 basePosition)
        {
            semaphoreBp.Wait();

            try
            {
                basePositionCache.Value.Add(basePosition);
            }
            finally { semaphoreBp.Release(); }
        }
    }
}
