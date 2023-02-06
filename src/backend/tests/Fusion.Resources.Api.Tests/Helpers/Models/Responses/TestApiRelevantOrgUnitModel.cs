using System.Collections.Generic;

namespace Fusion.Resources.Api.Tests.Helpers.Models.Responses
{
    public class TestApiRelevantOrgUnitModel
    {
        public string SapId { get; set; }
        public string FullDepartment { get; set; }
        public List<string> Reasons { get; set; } = new();
        public string Name { get; set; }
        public string ParentSapId { get; set; }
        public string ShortName { get; set; }
        public string Department { get; set; }


    }
}
