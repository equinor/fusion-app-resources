﻿using Fusion.Resources.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Controllers.Departments
{
    [ApiVersion("1.0-preview")]
    [Authorize]
    [ApiController]
    public class DepartmentsController : ResourceControllerBase
    {
        [HttpGet("/departments/{departmentString}")]
        public async Task<ActionResult<ApiDepartment>> GetDepartments(string departmentString)
        {
            var request = new GetDepartments()
                .ById(departmentString)
                .ExpandResourceOwners();

            var departments = await DispatchAsync(request);

            return Ok(new ApiDepartment(departments.Single()));
        }

        [HttpGet("/departments")]
        public async Task<ActionResult<List<ApiDepartment>>> Search([FromQuery(Name = "$search")] string query)
        {
            var request = new GetDepartments()
                .ExpandDelegatedResourceOwners()
                .ExpandResourceOwners()
                .WhereResourceOwnerMatches(query);

            var result = await DispatchAsync(request);

            return Ok(result.Select(x => new ApiDepartment(x)));
        }
    }
}