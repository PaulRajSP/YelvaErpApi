using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Repository;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Services
{
    public class PaymentTransactionService
    {
        private readonly IPaymentTransactionRepository _repo;
        public PaymentTransactionService(IPaymentTransactionRepository repo) => _repo = repo;

        public PageResult<PaymentTransactionListItem> GetAll(GetPaymentTransactionListReq req) => _repo.GetAll(req);

        public GetPaymentTransactionBalanceRes GetBalance() => new GetPaymentTransactionBalanceRes { Balance = _repo.GetBalance() };

        public CreatePaymentTransactionRes Create(CreatePaymentTransactionReq req)
        {
            if (req.Amount <= 0)
                throw new HoneyErpException(ErrorCode.InvalidData, "Amount must be greater than zero.");

            req.TransactionNumber = Utilities.GeneratePublicId("TXN", 5);

            return _repo.Create(req);
        }
    }
}
