using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Repository;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Services
{
    public class EmployeeService
    {
        private readonly IEmployeeRepository _repo;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<EmployeeService> _logger;

        public EmployeeService(IEmployeeRepository repo, ICurrentUserService currentUserService, ILogger<EmployeeService> logger)
        {
            _repo = repo;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public PageResult<EmployeeListItem> GetAllEmployees(GetEmployeeListReq req)
        {
            return _repo.GetAllEmployees(req);
        }

        public EmployeeProfileRes GetEmployeeById(int employeeId)
        {
            var emp = _repo.GetEmployeeById(employeeId);
            if (emp == null)
            {
                throw new HoneyErpException(ErrorCode.NotFound, $"Employee with ID {employeeId} not found.");
            }
            return emp;
        }

        public CreateEmployeeRes CreateEmployee(CreateEmployeeReq req)
        {
            if (string.IsNullOrWhiteSpace(req.EmployeeName))
                throw new HoneyErpException(ErrorCode.ValidationFailed, "Employee name is required.");

            if (string.IsNullOrWhiteSpace(req.Password))
                throw new HoneyErpException(ErrorCode.ValidationFailed, "Password is required for new employees.");

            // Generate employee code with random using Utilities package if not provided
            if (string.IsNullOrWhiteSpace(req.EmployeeCode))
            {
                req.EmployeeCode = _repo.GenerateRandomEmployeeCode();
            }
            else if (_repo.IsEmployeeCodeExists(req.EmployeeCode))
            {
                throw new HoneyErpException(ErrorCode.Conflict, $"Employee code '{req.EmployeeCode}' is already registered.");
            }

            PasswordHasher.CreatePasswordHash(req.Password, out string hash, out string salt);

            string createdBy = _currentUserService.EmployeeCode ?? "SYSTEM";
            int employeeId = _repo.CreateEmployee(req, hash, salt, createdBy);
            _logger.LogInformation("New employee created: {Code} with ID {Id} by {CreatedBy}", req.EmployeeCode, employeeId, createdBy);

            return new CreateEmployeeRes
            {
                EmployeeId = employeeId,
                EmployeeCode = req.EmployeeCode
            };
        }

        public void UpdateEmployee(UpdateEmployeeReq req)
        {
            if (req.EmployeeId <= 0)
                throw new HoneyErpException(ErrorCode.ValidationFailed, "Invalid employee ID.");

            if (string.IsNullOrWhiteSpace(req.EmployeeName))
                throw new HoneyErpException(ErrorCode.ValidationFailed, "Employee name cannot be empty.");

            var existing = _repo.GetEmployeeById(req.EmployeeId);
            if (existing == null)
                throw new HoneyErpException(ErrorCode.NotFound, $"Employee with ID {req.EmployeeId} not found.");

            string? newHash = null;
            string? newSalt = null;

            if (!string.IsNullOrWhiteSpace(req.NewPassword))
            {
                PasswordHasher.CreatePasswordHash(req.NewPassword, out newHash, out newSalt);
                _logger.LogInformation("Password reset for employee ID {Id}", req.EmployeeId);
            }

            _repo.UpdateEmployee(req, newHash, newSalt);
            _logger.LogInformation("Employee updated: ID {Id}", req.EmployeeId);
        }

        public List<string> GetAvailableRoles()
        {
            return _repo.GetAvailableRoles();
        }
    }
}
