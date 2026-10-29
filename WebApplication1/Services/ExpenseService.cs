using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Repository;
using AttachmentEnum;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Services
{
    public class ExpenseService
    {
        private readonly IExpenseRepository _repo;
        private readonly ICurrentUserService _currentUserService;
        private readonly CommonService _commonService;

        public ExpenseService(IExpenseRepository repo, ICurrentUserService currentUserService, CommonService commonService)
        {
            _repo = repo;
            _currentUserService = currentUserService;
            _commonService = commonService;
        }

        public PageResult<ExpenseItemDto> GetAll(GetExpenseListReq req)
        {
            PageResult<ExpenseItemDto> result = _repo.GetAll(req);
            foreach (ExpenseItemDto item in result.Items)
                LoadAttachments(item);
            return result;
        }

        public ExpenseItemDto GetById(string edid)
        {
            var expense = _repo.GetById(edid);
            if (expense == null)
                throw new HoneyErpException(ErrorCode.NotFound, "Expense record not found.");
            LoadAttachments(expense);
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
            string edid = _repo.Create(req);
            if (req.AttachmentData != null)
                _commonService.SetModuleAttachments(req.AttachmentData, AttachmentModule.Expense, edid);
            return edid;
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

            if (req.AttachmentData != null)
                _commonService.SetModuleAttachments(req.AttachmentData, AttachmentModule.Expense, edid);

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

        private void LoadAttachments(ExpenseItemDto expense)
        {
            expense.AttachmentData = _commonService.GetModuleAttachments(AttachmentModule.Expense, expense.Edid);
            if (expense.AttachmentData.Count == 0 && !string.IsNullOrWhiteSpace(expense.LegacyDocEdid))
            {
                try
                {
                    AttachmentModel legacy = _commonService.GetAttachment(expense.LegacyDocEdid, string.Empty);
                    expense.AttachmentData.Add(new AttachmentModuleRes
                    {
                        AttachmentEdid = expense.LegacyDocEdid,
                        AttachmentOriginalName = legacy.OriginalName
                    });
                }
                catch { }
            }
        }
    }
}
