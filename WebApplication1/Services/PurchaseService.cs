using AttachmentEnum;
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
        private readonly CommonService _commonService;

        public PurchaseService(IPurchaseRepository repo, PaymentTransactionService paymentService, RawMaterialService rawMaterialService, CommonService commonService)
        {
            _repo = repo;
            _paymentService = paymentService;
            _rawMaterialService = rawMaterialService;
            _commonService = commonService;
        }

        public PageResult<PurchaseListItem> GetAll(GetPurchaseListReq req)
        {
            PageResult<PurchaseListItem> result = _repo.GetAll(req);
            foreach (PurchaseListItem item in result.Items)
                LoadAttachments(item);
            return result;
        }

        public GetPurchaseListRes GetBySupplier(GetPurchasesBySupplierReq req)
        {
            GetPurchaseListRes result = _repo.GetBySupplier(req);
            foreach (PurchaseListItem item in result.Items)
                LoadAttachments(item);
            return result;
        }

        public GetPurchaseByIdRes GetById(string edid)
        {
            GetPurchaseByIdRes? purchase = _repo.GetById(edid);
            if (purchase == null)
                throw new HoneyErpException(ErrorCode.NotFound, "Purchase not found.");

            LoadAttachments(purchase);
            purchase.Payments = _repo.GetPayments(purchase.Edid);
            foreach (PurchasePaymentItem payment in purchase.Payments)
                payment.AttachmentData = _commonService.GetModuleAttachments(AttachmentModule.PurchasePayment, payment.Edid);

            return purchase;
        }

        public PayPurchaseDueRes PayDue(PayPurchaseDueReq req)
        {
            if (string.IsNullOrWhiteSpace(req.PurchaseEdid))
                throw new HoneyErpException(ErrorCode.InvalidData, "Purchase ID is required.");
            if (req.Amount <= 0)
                throw new HoneyErpException(ErrorCode.InvalidData, "Amount must be greater than zero.");

            GetPurchaseByIdRes purchase = _repo.GetById(req.PurchaseEdid)
                ?? throw new HoneyErpException(ErrorCode.NotFound, "Purchase not found.");

            if (req.Status == OrderPaymentStatus.Success && req.Amount > purchase.Balance)
                throw new HoneyErpException(ErrorCode.InvalidData, "Amount exceeds due balance.");
            if (req.Status == OrderPaymentStatus.Refunded && req.Amount > purchase.PaidAmount)
                throw new HoneyErpException(ErrorCode.InvalidData, "Refund exceeds the amount already paid.");
            if (purchase.Balance <= 0 && req.Status != OrderPaymentStatus.Refunded)
                throw new HoneyErpException(ErrorCode.InvalidData, "No due balance.");

            decimal paidAmountDelta = req.Status switch
            {
                OrderPaymentStatus.Success => req.Amount,
                OrderPaymentStatus.Refunded => -req.Amount,
                _ => 0m
            };
            decimal updatedPaidAmount = purchase.PaidAmount + paidAmountDelta;
            string newStatus = updatedPaidAmount <= 0
                ? "UNPAID"
                : updatedPaidAmount >= purchase.TotalAmount ? "PAID" : "PARTIAL";

            string transactionNumber = Utilities.GeneratePublicId("TXN", 6);
            int paymentId = _repo.RecordPayment(req.PurchaseEdid, purchase.SupplierEdid, req, newStatus, paidAmountDelta, transactionNumber);
            if (paymentId <= 0)
                throw new HoneyErpException(ErrorCode.InternalServerError, "Failed to record payment.");

            string paymentEdid = EncryptionHelper.Encrypt(paymentId.ToString());
            if (req.AttachmentData != null)
                _commonService.SetModuleAttachments(req.AttachmentData, AttachmentModule.PurchasePayment, paymentEdid);

            if (req.Status == OrderPaymentStatus.Success || req.Status == OrderPaymentStatus.Refunded)
            {
                _paymentService.Create(new CreatePaymentTransactionReq
                {
                        TransactionNumber = transactionNumber,
                    Amount = req.Amount,
                    Description = $"Purchase #{purchase.PublicId} payment {req.Status.ToString().ToLowerInvariant()}",
                    Status = req.Status == OrderPaymentStatus.Refunded ? PaymentType.Credit : PaymentType.Debit,
                    Category = TransactionCategory.Purchase,
                    IsActive = true
                });
            }

            return new PayPurchaseDueRes
            {
                PurchaseEdid = req.PurchaseEdid,
                PaymentEdid = paymentEdid,
                    TransactionNumber = transactionNumber,
                AttachmentData = _commonService.GetModuleAttachments(AttachmentModule.PurchasePayment, paymentEdid)
            };
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
            if (req.Purchase.PaidAmount > 0)
                req.Purchase.InitialPaymentType = req.Purchase.PaidAmount >= req.Purchase.TotalAmount ? OrderPaymentType.FullPayment : OrderPaymentType.Advance;
            req.Purchase.PublicId = Utilities.GeneratePublicId("PUR", 4);

            CreatePurchaseRes result = _repo.Create(req);
            if (req.Purchase.AttachmentData != null)
                _commonService.SetModuleAttachments(req.Purchase.AttachmentData, AttachmentModule.Purchase, result.Edid);
            if (req.Purchase.InitialPaymentAttachmentData != null && !string.IsNullOrWhiteSpace(result.InitialPaymentEdid))
                _commonService.SetModuleAttachments(req.Purchase.InitialPaymentAttachmentData, AttachmentModule.PurchasePayment, result.InitialPaymentEdid);

            if (req.Purchase.PaidAmount > 0)
            {
                _paymentService.Create(new CreatePaymentTransactionReq
                {
                    TransactionNumber = result.InitialPaymentTransactionNumber,
                    Amount = req.Purchase.PaidAmount,
                    Description = $"Initial Payment of {req.Purchase.PaidAmount} to Purchase #{req.Purchase.PublicId}",
                    Status = PaymentType.Debit,
                    Category = TransactionCategory.Purchase,
                    IsActive = true
                });
            }

            return result;
        }

        public void UpdateStatus(string edid, string status)
        {
            if (!_repo.UpdateStatus(edid, status))
                throw new HoneyErpException(ErrorCode.NotFound, "Purchase not found.");
        }

        public GetPurchaseItemsRes GetItems(GetPurchaseItemsReq req) => _repo.GetItems(req);

        private void LoadAttachments(PurchaseListItem purchase)
        {
            purchase.AttachmentData = _commonService.GetModuleAttachments(AttachmentModule.Purchase, purchase.Edid);
        }

        private void LoadAttachments(GetPurchaseByIdRes purchase)
        {
            purchase.AttachmentData = _commonService.GetModuleAttachments(AttachmentModule.Purchase, purchase.Edid);
        }
    }
}