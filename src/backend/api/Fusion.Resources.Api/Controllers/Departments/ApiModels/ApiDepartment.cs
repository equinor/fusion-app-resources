using Fusion.Resources.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Api.Controllers.Departments
{
    public class ApiDepartment
    {
        public ApiDepartment(DbDepartment department)
        {
            Id = department.Id;
            OrgPath = department.OrgPath;
            Responsible = department.Responsible;
            OrgType = department.OrgType.FromDbType();

            if (department.Sector != null)
            {
                Sector = department.Sector.OrgPath;
            }

            if (department.Children != null)
            {
                Children = department.Children.Select(dpt => new ApiDepartment(dpt)).ToList();
            }
        }

        public Guid Id { get; }
        public string OrgPath { get; }
        public Guid Responsible { get; }
        public OrgTypes OrgType { get; }
        public string? Sector { get; }
        public List<ApiDepartment>? Children { get; } = new List<ApiDepartment>();
        public List<ApiInternalPersonnelPerson>? DepartmentPersonell { get; set; } = new List<ApiInternalPersonnelPerson>();

    }
}