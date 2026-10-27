using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Repository;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Services
{
    public class PurchaseService
    {
        private readonly IPurchaseRepository _repo;
        private readonly PaymentTransactionService _paymentService;
        private readonly RawMaterialService _rawMaterialService;

        public PurchaseService(IPurchaseRepository repo, PaymentTransactionService paymentService, RawMaterialService rawMaterialService) 
        {
            _repo = repo;
            _paymentService = paymentService;
            _rawMaterialService = rawMaterialService;
        }

        public PageResult<PurchaseListItem> GetAll(GetPurchaseListReq req) => _repo.GetAll(req);

        public GetPurchaseListRes GetBySupplier(GetPurchasesBySupplierReq req) => _repo.GetBySupplier(req);

        public GetPurchaseByIdRes GetById(string edid)
        {
            GetPurchaseByIdRes? p = _repo.GetById(edid);
            if (p == null) throw new HoneyErpException(ErrorCode.NotFound, "Purchase not found.");
            return p;
        }

        public PayPurchaseDueRes PayDue(PayPurchaseDueReq req)
        {
            if (string.IsNullOrWhiteSpace(req.PurchaseEdid))
                throw new HoneyErpException(ErrorCode.InvalidData, "Purchase ID is required.");
            if (req.Amount <= 0)
                throw new HoneyErpException(ErrorCode.InvalidData, "Amount must be greater than zero.");

            GetPurchaseByIdRes purchase = _repo.GetById(req.PurchaseEdid);
            if (purchase == null) throw new HoneyErpException(ErrorCode.NotFound, "Purchase not found.");

            if (purchase.Balance <= 0)
                throw new HoneyErpException(ErrorCode.InvalidData, "No due balance.");

            if (req.Amount > purchase.Balance)
                throw new HoneyErpException(ErrorCode.InvalidData, "Amount exceeds due balance.");

            string newStatus = (purchase.Balance - req.Amount) <= 0 ? "PAID" : "PARTIAL";

            bool success = _repo.RecordPayment(req.PurchaseEdid, purchase.SupplierEdid, req.Amount, newStatus);
            if (success)
            {
                _paymentService.Create(new CreatePaymentTransactionReq
                {
                    Amount = req.Amount,
                    Description = $"Clearing Due to {req.Amount} ruppe to Order id {purchase.PublicId}",
                    Status = PaymentType.Debit,
                    Category = TransactionCategory.Purchase,
                    IsActive = true
                });

                return new PayPurchaseDueRes { PurchaseEdid = req.PurchaseEdid };
            }
            throw new HoneyErpException(ErrorCode.InternalServerError, "Failed to record payment.");
        }

        public CreatePurchaseRes Create(CreatePurchaseReq req)
        {
            if (string.IsNullOrWhiteSpace(req.Purchase.SupplierEdid))
                throw new HoneyErpException(ErrorCode.InvalidData, "Supplier is required.");

            if (req.Items == null || req.Items.Count == 0)
                throw new HoneyErpException(ErrorCode.InvalidData, "Purchase must have at least one item.");

            decimal calculatedTotal = req.Items.Sum(x => x.Total);
            if (req.Purchase.TotalAmount != calculatedTotal)
                throw new HoneyErpException(ErrorCode.InvalidData, $"Total amount ({req.Purchase.TotalAmount}) does not match the sum of item totals ({calculatedTotal}).");

            if (req.Purchase.PaidAmount >= req.Purchase.TotalAmount && req.Purchase.TotalAmount > 0)
                req.Purchase.Status = PurchaseStatus.Paid;
            else if (req.Purchase.PaidAmount > 0)
                req.Purchase.Status = PurchaseStatus.Partial;
            else
                req.Purchase.Status = PurchaseStatus.Unpaid;

            req.Purchase.Balance = req.Purchase.TotalAmount - req.Purchase.PaidAmount;

            req.Purchase.PublicId = Utilities.GeneratePublicId("PUR", 4);

            CreatePurchaseRes res = _repo.Create(req);
            
            if (req.Purchase.PaidAmount > 0)
            {
                _paymentService.Create(new CreatePaymentTransactionReq
                {
                    Amount = req.Purchase.PaidAmount,
                    Description = $"Initial Payment of {req.Purchase.PaidAmount} ruppe to Order id {req.Purchase.PublicId}",
                    Status = PaymentType.Debit,
                    Category = TransactionCategory.Purchase,
                    IsActive = true
                });
            }

            return res;
        }

        public void UpdateStatus(string edid, string status)
        {
            if (!_repo.UpdateStatus(edid, status))
                throw new HoneyErpException(ErrorCode.NotFound, "Purchase not found.");
        }

        public GetPurchaseItemsRes GetItems(GetPurchaseItemsReq req) => _repo.GetItems(req);

    }
}
