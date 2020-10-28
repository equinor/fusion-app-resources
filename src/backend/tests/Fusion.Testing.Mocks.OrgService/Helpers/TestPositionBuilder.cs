using Fusion.ApiClients.Org;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Fusion.Testing.Mocks.OrgService
{
    public static class PositionBuilder
    {

        public static ApiPositionV2 NewPosition()
        {
            return CreateTestPosition()
                .Generate();
        }

        public static Bogus.Faker<ApiPositionV2> CreateTestPosition()
        {
            return new Bogus.Faker<ApiPositionV2>()
                .RuleFor(p => p.Id, f => Guid.NewGuid())
                .RuleFor(p => p.Properties, f => new ApiPositionPropertiesV2())
                .RuleFor(p => p.ExternalId, f => f.Random.AlphaNumeric(5))
                .RuleFor(p => p.BasePosition, f => f.PickRandom(AllBasePositions.Except(new[] { DirectorBasePosition })))
                .RuleFor(p => p.Instances, f => PositionInstanceBuilder.CreateInstanceStack(f, 4))
                .FinishWith((f, p) =>
                {
                    p.Name = p.BasePosition.Name;
                    p.Instances.ForEach(i => i.PositionId = p.Id);
                });
        }

        
        private static List<ApiBasePositionV2> basePositionCache = null;
        public static List<ApiBasePositionV2> AllBasePositions
        {
            get
            {
                if (basePositionCache == null)
                {
                    var bps = GetBasePositionCSVImport();
                    basePositionCache = bps.Select(bp => new ApiBasePositionV2
                    {
                        Id = bp.Id,
                        Department = bp.Department,
                        Discipline = bp.Discipline,
                        Name = bp.Name,
                        Inactive = bp.Inactive,
                        ProjectType = bp.ProjectType
                    }).ToList();
                }

                return basePositionCache;
            }
        }

        public static ApiBasePositionV2 DirectorBasePosition => AllBasePositions.First(bp => bp.Id == new Guid("f942a973-9cac-4a96-8bc7-a9e41141f021"));

        private static List<ApiBasePositionV2> GetBasePositionCSVImport()
        {
            var resourceData = AssemblyUtils.GetResourceFromCurrentAssembly($@"Fusion.Testing.Mocks.OrgService.Data.BasePositions.csv");
            var data = resourceData.Split(new [] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

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
                Discipline = (tokens[2] ?? "").Trim(),
                Department = (tokens[3] ?? "").Trim(),
                Inactive = bool.Parse(tokens[4])
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
    }

    

}
