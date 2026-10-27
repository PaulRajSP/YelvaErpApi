using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static HoneyErpAPI.Models.UpdateEmployeeReq;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Controllers
{
    [ApiController]
    [Route("api/employees")]
    public class EmployeeController : ControllerBase
    {
        private readonly EmployeeService _service;
        private readonly ILogger<EmployeeController> _logger;

        public EmployeeController(EmployeeService service, ILogger<EmployeeController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost("list")]
        public IActionResult GetAllEmployees([FromBody] GetEmployeeListReq req)
        {
            var res = new ApiResponseModel<PageResult<EmployeeListItem>>
            {
                Success = true,
                Data = _service.GetAllEmployees(req)
            };
            return Ok(res);
        }

        [HttpGet("{id:int}")]
        public IActionResult GetEmployeeById(int id)
        {
            var res = new ApiResponseModel<EmployeeProfileRes>
            {
                Success = true,
                Data = _service.GetEmployeeById(id)
            };
            return Ok(res);
        }

        [HttpPost]
        public IActionResult CreateEmployee([FromBody] CreateEmployeeReq req)
        {
            var data = _service.CreateEmployee(req);
            var res = new ApiResponseModel<CreateEmployeeRes>
            {
                Success = true,
                Message = $"Employee {data.EmployeeCode} created successfully",
                Data = data
            };
            return Ok(res);
        }

        [HttpPut("{id:int}")]
        public IActionResult UpdateEmployee(int id, [FromBody] UpdateEmployeeReq req)
        {
            req.EmployeeId = id;
            _service.UpdateEmployee(req);
            var res = new ApiResponseModel<UpdateEmployeeRes>
            {
                Success = true,
                Message = "Employee updated successfully",
                Data = new UpdateEmployeeRes { EmployeeId = id }
            };
            return Ok(res);
        }

        [HttpGet("roles")]
        public IActionResult GetAvailableRoles()
        {
            var roles = _service.GetAvailableRoles();
            var res = new ApiResponseModel<GetRolesRes>
            {
                Success = true,
                Data = new GetRolesRes { Roles = roles }
            };
            return Ok(res);
        }
    }
}
