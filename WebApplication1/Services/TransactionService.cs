using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Repository;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Services
{
    public class TransactionService
    {
        private readonly ITransactionRepository _repo;
        public TransactionService(ITransactionRepository repo) => _repo = repo;

        public PageResult<TransactionListItem> GetAll(GetTransactionListReq req) => _repo.GetAll(req);

        public PageResult<TransactionListItem> GetByCategory(string category, GetTransactionListReq req) => _repo.GetByCategory(category, req);

        public GetTransactionByIdRes GetById(string edid)
        {
            GetTransactionByIdRes? txn = _repo.GetById(edid);
            if (txn == null) throw new HoneyErpException(ErrorCode.NotFound, "Transaction not found.");
            return txn;
        }

        public CreateTransactionRes Create(CreateTransactionReq req)
        {
            if (req.Amount <= 0)
                throw new HoneyErpException(ErrorCode.InvalidData, "Amount must be greater than zero.");
            if (string.IsNullOrWhiteSpace(req.Type))
                throw new HoneyErpException(ErrorCode.InvalidData, "Type (CREDIT/DEBIT) is required.");
            if (string.IsNullOrWhiteSpace(req.Category))
                throw new HoneyErpException(ErrorCode.InvalidData, "Category is required.");

            string id = _repo.Create(req);
            return new CreateTransactionRes { Edid = EncryptionHelper.Encrypt(id) };
        }
    }
}
