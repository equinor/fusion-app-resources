using System;

namespace Fusion.Resources.Api.Integrations
{
    public class Department
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public DepartmentRef Parent { get; set; }
        public DepartmentRef[] Children { get; set; }
        public Manager Manager { get; set; }
    }

    public class Manager
    {
        public string AzureUniqueId { get; set; }
        public string Department { get; set; }
        public string FullDepartment { get; set; }
        public string Name { get; set; }
        public string GivenName { get; set; }
        public string Surname { get; set; }
        public string JobTitle { get; set; }
        public string Mail { get; set; }
        public string Country { get; set; }
        public string Phone { get; set; }
        public string OfficeLocation { get; set; }
        public string UserType { get; set; }
        public bool IsResourceOwner { get; set; }
        public bool HasOfficeLicense { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastSyncDate { get; set; }
    }

    public class DepartmentRef
    {
        public string Name { get; set; }
        public string FullName { get; set; }
    }
}
