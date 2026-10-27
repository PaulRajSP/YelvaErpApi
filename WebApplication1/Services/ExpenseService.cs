using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Repository;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Services
{
    public class ExpenseService
    {
        private readonly IExpenseRepository _repo;
        private readonly ICurrentUserService _currentUserService;

        public ExpenseService(IExpenseRepository repo, ICurrentUserService currentUserService)
        {
            _repo = repo;
            _currentUserService = currentUserService;
        }

        public PageResult<ExpenseItemDto> GetAll(GetExpenseListReq req)
        {
            return _repo.GetAll(req);
        }

        public ExpenseItemDto GetById(string edid)
        {
            var expense = _repo.GetById(edid);
            if (expense == null)
                throw new HoneyErpException(ErrorCode.NotFound, "Expense record not found.");
            return expense;
        }

        public string Create(CreateExpenseReq req)
        {
            if (string.IsNullOrWhiteSpace(req.Title))
                throw new HoneyErpException(ErrorCode.InvalidData, "Expense title is required.");

            if (req.Amount <= 0)
                throw new HoneyErpException(ErrorCode.InvalidData, "Expense amount must be greater than zero.");

            if (string.IsNullOrWhiteSpace(req.Category))
                throw new HoneyErpException(ErrorCode.InvalidData, "Expense category is required.");

            req.RecordedBy = _currentUserService.EmployeeCode ?? req.RecordedBy ?? "SYSTEM";
            return _repo.Create(req);
        }

        public bool Update(string edid, UpdateExpenseReq req)
        {
            if (string.IsNullOrWhiteSpace(req.Title))
                throw new HoneyErpException(ErrorCode.InvalidData, "Expense title is required.");

            if (req.Amount <= 0)
                throw new HoneyErpException(ErrorCode.InvalidData, "Expense amount must be greater than zero.");

            bool updated = _repo.Update(edid, req);
            if (!updated)
                throw new HoneyErpException(ErrorCode.NotFound, "Expense record not found or could not be updated.");

            return true;
        }

        public bool Delete(string edid)
        {
            bool deleted = _repo.Delete(edid);
            if (!deleted)
                throw new HoneyErpException(ErrorCode.NotFound, "Expense record not found or could not be deleted.");

            return true;
        }

        public ExpenseSummaryRes GetSummary(DateTime? fromDate, DateTime? toDate)
        {
            return _repo.GetSummary(fromDate, toDate);
        }
    }
}
