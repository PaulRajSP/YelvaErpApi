using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HoneyErpAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _service;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AuthService service, ILogger<AuthController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public IActionResult Login([FromBody] LoginReq req)
        {
            var res = new ApiResponseModel<LoginRes>
            {
                Success = true,
                Data = _service.Login(req),
                Message = "Login successful"
            };

            return Ok(res);
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            var empIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(empIdStr) || !int.TryParse(empIdStr, out int empId))
            {
                return Unauthorized(new ApiResponseModel<EmployeeProfileRes>
                {
                    Success = false,
                    Message = "Invalid or expired user session."
                });
            }

            var res = new ApiResponseModel<EmployeeProfileRes>
            {
                Success = true,
                Data = _service.GetCurrentEmployeeProfile(empId),
                Message = "Profile retrieved successfully"
            };

            return Ok(res);
        }

        [HttpGet("menus")]
        [Authorize]
        public IActionResult GetMyMenus()
        {
            var empIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(empIdStr) || !int.TryParse(empIdStr, out int empId))
            {
                return Unauthorized(new { success = false, message = "Invalid token." });
            }

            var menus = _service.GetMenusForEmployee(empId);
            return Ok(new { success = true, data = menus });
        }
    }
}
