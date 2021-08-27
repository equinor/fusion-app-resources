using Bogus;
using Fusion.Testing.Mocks.LineOrgService;
using Fusion.Testing.Mocks.ProfileService;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Resources.Api
{
    public static class InternalRequestData
    {
        public static Faker Faker { get; } = new Faker();

        public static List<string> SupportedDepartments { get; }
        public static string RandomDepartment => Faker.PickRandom(SupportedDepartments);
        public static string PickRandomDepartment(params string[] exceptFor) => Faker.PickRandom(SupportedDepartments.Where(d => !exceptFor.Any(ed => string.Equals(ed, d, StringComparison.OrdinalIgnoreCase))));

        static InternalRequestData()
        {
            var data = File.ReadAllText("Data/departmentSectors.json");

            var sectorInfo = JsonConvert.DeserializeAnonymousType(data, new[] { new { sector = string.Empty, departments = Array.Empty<string>() } });

            SupportedDepartments = sectorInfo.SelectMany(s => s.departments).Union(sectorInfo.Select(s => s.sector)).ToList();

            foreach (var department in SupportedDepartments)
            {
                var user = PeopleServiceMock.AddTestProfile().SaveProfile();
                LineOrgServiceMock.AddTestUser().MergeWithProfile(user).AsResourceOwner().WithFullDepartment(department).SaveProfile();
            }
        }
    }
}
