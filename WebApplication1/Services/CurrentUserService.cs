using System.Security.Claims;

namespace HoneyErpAPI.Services
{
    public interface ICurrentUserService
    {
        int? EmployeeId { get; }
        string? EmployeeCode { get; }
        string? EmployeeName { get; }
        string? Email { get; }
        List<string> Roles { get; }
        bool IsAuthenticated { get; }
    }

    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public int? EmployeeId
        {
            get
            {
                var idStr = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("sub")?.Value;
                return int.TryParse(idStr, out int id) ? id : null;
            }
        }

        public string? EmployeeCode =>
            User?.FindFirst(ClaimTypes.Name)?.Value
            ?? User?.FindFirst("unique_name")?.Value
            ?? User?.FindFirst("employeeCode")?.Value;

        public string? EmployeeName =>
            User?.FindFirst(ClaimTypes.GivenName)?.Value
            ?? User?.FindFirst("name")?.Value;

        public string? Email =>
            User?.FindFirst(ClaimTypes.Email)?.Value;

        public List<string> Roles =>
            User?.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct().ToList()
            ?? new List<string>();

        public bool IsAuthenticated =>
            User?.Identity?.IsAuthenticated ?? false;
    }
}
