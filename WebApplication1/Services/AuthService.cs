using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Repository;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HoneyErpAPI.Services
{
    public class AuthService
    {
        private readonly IEmployeeRepository _repo;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IEmployeeRepository repo, IConfiguration config, ILogger<AuthService> logger)
        {
            _repo = repo;
            _config = config;
            _logger = logger;
        }

        public LoginRes Login(LoginReq req)
        {
            if (string.IsNullOrWhiteSpace(req.EmployeeCode) || string.IsNullOrWhiteSpace(req.Password))
            {
                throw new HoneyErpException(ErrorCode.ValidationFailed, "Employee code and password are required.");
            }

            EmployeeRecordDb? emp = _repo.GetEmployeeRecordByCode(req.EmployeeCode.Trim());
            if (emp == null)
            {
                _logger.LogWarning("Failed login attempt: employee {Code} not found.", req.EmployeeCode);
                throw new HoneyErpException(ErrorCode.Unauthorized, "Invalid employee code or password.");
            }

            if (!emp.IsActive)
            {
                _logger.LogWarning("Login attempt for deactivated employee {Code}.", req.EmployeeCode);
                throw new HoneyErpException(ErrorCode.Forbidden, "Your account has been deactivated. Please contact your administrator.");
            }

            bool isPasswordValid = PasswordHasher.VerifyPasswordHash(req.Password, emp.PasswordHash, emp.PasswordSalt);
            if (!isPasswordValid)
            {
                _logger.LogWarning("Failed login attempt: incorrect password for {Code}.", req.EmployeeCode);
                throw new HoneyErpException(ErrorCode.Unauthorized, "Invalid employee code or password.");
            }

            // Fetch roles, permissions, and menus
            List<string> roles = _repo.GetEmployeeRoles(emp.EmployeeId);
            if (roles.Count == 0)
            {
                // Fallback default role
                roles.Add(RoleEnum.Staff.ToString());
            }

            List<string> permissions = _repo.GetPermissionsForRoles(roles);
            List<MenuItemDto> menus = _repo.GetMenusForPermissions(permissions);

            // Generate JWT Token
            string token = GenerateJwtToken(emp, roles);

            return new LoginRes
            {
                Token = token,
                Employee = new EmployeeProfileRes
                {
                    EmployeeId = emp.EmployeeId,
                    EmployeeCode = emp.EmployeeCode,
                    EmployeeName = emp.EmployeeName,
                    Email = emp.Email,
                    Mobile = emp.Mobile,
                    DocumentIdJson = emp.DocumentIdJson,
                    IsActive = emp.IsActive,
                    Roles = roles
                },
                Permissions = permissions,
                Menus = menus
            };
        }

        public EmployeeProfileRes GetCurrentEmployeeProfile(int employeeId)
        {
            var profile = _repo.GetEmployeeById(employeeId);
            if (profile == null)
            {
                throw new HoneyErpException(ErrorCode.NotFound, "Employee not found.");
            }
            return profile;
        }

        public List<MenuItemDto> GetMenusForEmployee(int employeeId)
        {
            List<string> roles = _repo.GetEmployeeRoles(employeeId);
            List<string> permissions = _repo.GetPermissionsForRoles(roles);
            return _repo.GetMenusForPermissions(permissions);
        }

        private string GenerateJwtToken(EmployeeRecordDb emp, List<string> roles)
        {
            var keyString = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
            var issuer = _config["Jwt:Issuer"] ?? "HoneyErpAPI";
            var audience = _config["Jwt:Audience"] ?? "HoneyErpUsers";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, emp.EmployeeId.ToString()),
                new Claim(ClaimTypes.Name, emp.EmployeeCode),
                new Claim(ClaimTypes.GivenName, emp.EmployeeName ?? emp.EmployeeCode),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (!string.IsNullOrWhiteSpace(emp.Email))
            {
                claims.Add(new Claim(ClaimTypes.Email, emp.Email));
            }

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7), // 7-day token
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
