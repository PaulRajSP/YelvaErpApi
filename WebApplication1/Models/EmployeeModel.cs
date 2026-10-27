using HoneyErpAPI.Common;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Models
{
    public class LoginReq
    {
        public string EmployeeCode { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class MenuItemDto
    {
        public int Id { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public string MenuName { get; set; } = string.Empty;
        public string PermissionEnum { get; set; } = string.Empty;
        public string FrontendUrl { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class EmployeeProfileRes : BaseApiResponse
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? DocumentIdJson { get; set; }
        public bool IsActive { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public string? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class LoginRes : BaseApiResponse
    {
        public string Token { get; set; } = string.Empty;
        public EmployeeProfileRes Employee { get; set; } = new EmployeeProfileRes();
        public List<string> Permissions { get; set; } = new List<string>();
        public List<MenuItemDto> Menus { get; set; } = new List<MenuItemDto>();
    }

    public class EmployeeListItem
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? DocumentIdJson { get; set; }
        public bool IsActive { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public string? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class GetEmployeeListReq
    {
        public PaginationRequest Pagination { get; set; } = new PaginationRequest();
        public string? SearchTerm { get; set; }
        public string? Role { get; set; }
        public bool? IsActive { get; set; }
    }

    public class CreateEmployeeReq
    {
        public string? EmployeeCode { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? DocumentIdJson { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class UpdateEmployeeReq
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? DocumentIdJson { get; set; }
        public bool IsActive { get; set; } = true;
        public string? NewPassword { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class CreateEmployeeRes : BaseApiResponse
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
    }

    public class UpdateEmployeeRes : BaseApiResponse
    {
        public int EmployeeId { get; set; }
    }

    public class GetRolesRes : BaseApiResponse
    {
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class EmployeeRecordDb
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string PasswordSalt { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? DocumentIdJson { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
