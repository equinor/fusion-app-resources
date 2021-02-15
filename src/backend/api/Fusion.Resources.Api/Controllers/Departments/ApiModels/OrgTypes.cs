using System;

namespace Fusion.Resources.Api.Controllers.Departments
{
    public enum OrgTypes
    {
        Sector, 
        Department
    }

    public static class OrgTypesExtensions
    {
        public static Database.Entities.DbDepartment.OrgTypes ToDbType(this OrgTypes orgType)
        {
            switch (orgType)
            {
                case OrgTypes.Sector:
                    return Database.Entities.DbDepartment.OrgTypes.Sector;
                case OrgTypes.Department:
                    return Database.Entities.DbDepartment.OrgTypes.Department;
                default:
                    throw new NotSupportedException($"Unsupported org type: [{orgType}]");
            }
        }

        public static OrgTypes FromDbType(this Database.Entities.DbDepartment.OrgTypes orgType)
        {
            switch (orgType)
            {
                case Database.Entities.DbDepartment.OrgTypes.Sector:
                    return OrgTypes.Sector;
                case Database.Entities.DbDepartment.OrgTypes.Department:
                    return OrgTypes.Department;
                default:
                    throw new NotSupportedException($"Unsupported org type: [{orgType}]");
            }
        }
    }
}
