using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Authorization;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Controllers.Departments
{
    [Route("[controller]")]
    [ApiController]
    public class DepartmentsController : ResourceControllerBase
    {
        private readonly ResourcesDbContext db;

        public DepartmentsController(ResourcesDbContext db)
        {
            this.db = db;
        }

        [HttpGet]
        public async Task<ActionResult<List<ApiDepartment>>> GetDepartments()
        {
            var departments = await db.Departments.ToListAsync();
            return departments.Select(dpt => new ApiDepartment(dpt)).ToList();
        }

        [HttpGet("{orgPath}")]
        public async Task<ActionResult<ApiDepartment>> GetSector(string orgPath)
        {
            var department = await db.Departments
                .SingleOrDefaultAsync(dpt => dpt.OrgPath == orgPath);
            if (department == null) return NotFound();


            return new ApiDepartment(department);
        }

        [HttpPost]
        public async Task<IActionResult> AddDepartment(CreateDepartment department)
        {
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AnyOf(or =>
                {
                    or.BeTrustedApplication();
                    or.FullControl();
                    // TODO: Figure out auth requirements
                });
            });

            var sector = await db.Departments.SingleOrDefaultAsync(dpt => dpt.OrgPath == department.SectorOrgPath);

            db.Departments.Add(new DbDepartment
            {
                Responsible = department.Responsible,
                OrgPath = department.OrgPath,
                OrgType = department.OrgType.ToDbType(),
                SectorId = sector?.Id
            });
            await db.SaveChangesAsync();
            return Ok();
        }

        [HttpPut("{orgPath}")]
        public async Task<IActionResult> UpdateSector(string orgPath, UpdateDepartment department)
        {
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AnyOf(or =>
                {
                    or.BeTrustedApplication();
                    or.FullControl();
                    // TODO: Figure out auth requirements
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            var existingDepartment = await db.Departments
                .SingleOrDefaultAsync(dpt => dpt.OrgPath == orgPath);

            if (existingDepartment == null) return NotFound();

            var sector = await db.Departments
                .SingleOrDefaultAsync(dpt => dpt.OrgPath == department.SectorOrgPath);

            existingDepartment.Responsible = department.Responsible;
            existingDepartment.OrgType = department.OrgType.ToDbType();
            existingDepartment.SectorId = sector?.Id;

            await db.SaveChangesAsync();
            return Ok();
        }
    }
}
