using System;
using System.Collections.Generic;
using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Repository;
using AttachmentEnum;

namespace HoneyErpAPI.Services
{
    public class OrderPaymentService
    {
        private readonly IOrderPaymentRepository _paymentRepo;
        private readonly ITransactionRepository _transactionRepo;
        private readonly IPaymentTransactionRepository _paymentTransactionRepo;
        private readonly CommonService _commonService;

        public OrderPaymentService(
            IOrderPaymentRepository paymentRepo,
            ITransactionRepository transactionRepo,
            IPaymentTransactionRepository paymentTransactionRepo,
            CommonService commonService)
        {
            _paymentRepo = paymentRepo;
            _transactionRepo = transactionRepo;
            _paymentTransactionRepo = paymentTransactionRepo;
            _commonService = commonService;
        }

        public RecordOrderPaymentRes RecordPayment(string orderEdid, RecordOrderPaymentReq req)
        {
            if (string.IsNullOrWhiteSpace(orderEdid))
                throw new HoneyErpException(ErrorCode.InvalidData, "Order ID is required.");

            if (req.Amount <= 0)
                throw new HoneyErpException(ErrorCode.InvalidData, "Payment amount must be greater than zero.");

            int orderId = Convert.ToInt32(EncryptionHelper.Decrypt(orderEdid));
            OrderHeaderSummary order = _paymentRepo.GetOrderHeader(orderId);
            if (order == null)
                throw new HoneyErpException(ErrorCode.NotFound, "Order not found.");

            decimal netPaidBefore = _paymentRepo.ComputeNetPaid(orderId);
            decimal unpaidBalance = Math.Max(0m, order.TotalAmount - netPaidBefore);

            if (req.Status == OrderPaymentStatus.Success && req.Amount > unpaidBalance && unpaidBalance > 0)
            {
                throw new HoneyErpException(
                    ErrorCode.InvalidData,
                    $"Payment amount ({req.Amount:N2}) exceeds remaining unpaid balance ({unpaidBalance:N2})."
                );
            }

            string transactionNumber = Utilities.GeneratePublicId("TXN", 6);
            DateTime paymentDate = req.PaymentDate ?? DateTime.UtcNow;

            int paymentId = _paymentRepo.InsertPayment(orderId, transactionNumber, req, paymentDate);
            string paymentEdid = EncryptionHelper.Encrypt(paymentId.ToString());
            if (req.AttachmentData != null)
                _commonService.SetModuleAttachments(MapAttachments(req.AttachmentData), AttachmentModule.OrderPayment, paymentEdid);

            decimal netPaidAfter = _paymentRepo.ComputeNetPaid(orderId);
            PaymentStatus newStatus;
            if (netPaidAfter <= 0)
                newStatus = PaymentStatus.Unpaid;
            else if (netPaidAfter >= order.TotalAmount)
                newStatus = PaymentStatus.Paid;
            else
                newStatus = PaymentStatus.PartiallyPaid;

            _paymentRepo.UpdateOrderPaymentStatus(orderId, newStatus);

            if (_paymentTransactionRepo != null)
            {
                if (req.Status == OrderPaymentStatus.Success)
                {
                    string txnDesc = $"Order #{order.OrderNumber} payment via {req.PaymentMethod}";
                    if (!string.IsNullOrWhiteSpace(req.PaymentReference))
                        txnDesc += $" (Ref: {req.PaymentReference})";

                    _paymentTransactionRepo.Create(new CreatePaymentTransactionReq
                    {
                        TransactionNumber = transactionNumber,
                        Description = txnDesc,
                        Amount = req.Amount,
                        Status = PaymentType.Credit,
                        Category = TransactionCategory.Sale,
                        IsActive = true
                    });
                }
                else if (req.Status == OrderPaymentStatus.Refunded)
                {
                    string txnDesc = $"Order #{order.OrderNumber} payment refund";
                    if (!string.IsNullOrWhiteSpace(req.PaymentReference))
                        txnDesc += $" (Ref: {req.PaymentReference})";

                    _paymentTransactionRepo.Create(new CreatePaymentTransactionReq
                    {
                        TransactionNumber = transactionNumber,
                        Description = txnDesc,
                        Amount = req.Amount,
                        Status = PaymentType.Debit,
                        Category = TransactionCategory.Refund,
                        IsActive = true
                    });
                }
            }

            return new RecordOrderPaymentRes
            {
                Edid = paymentEdid,
                TransactionNumber = transactionNumber,
                OrderId = orderId,
                OrderEdid = orderEdid,
                Amount = req.Amount,
                Status = req.Status.ToString(),
                TotalAmount = order.TotalAmount,
                AmountPaid = netPaidAfter,
                BalanceAmount = Math.Max(0m, order.TotalAmount - netPaidAfter),
                PaymentStatus = newStatus.ToString(),
                AttachmentData = _commonService.GetModuleAttachments(AttachmentModule.OrderPayment, paymentEdid)
            };
        }

        public GetOrderPaymentsRes GetPaymentsByOrderId(string orderEdid)
        {
            if (string.IsNullOrWhiteSpace(orderEdid))
                throw new HoneyErpException(ErrorCode.InvalidData, "Order ID is required.");

            int orderId = Convert.ToInt32(EncryptionHelper.Decrypt(orderEdid));
            OrderHeaderSummary order = _paymentRepo.GetOrderHeader(orderId);
            if (order == null)
                throw new HoneyErpException(ErrorCode.NotFound, "Order not found.");

            decimal netPaid = _paymentRepo.ComputeNetPaid(orderId);
            List<OrderPaymentItem> payments = _paymentRepo.GetPaymentsByOrderId(orderId, orderEdid);
            foreach (OrderPaymentItem payment in payments)
                LoadAttachments(payment);

            return new GetOrderPaymentsRes
            {
                OrderEdid = orderEdid,
                OrderNumber = order.OrderNumber,
                TotalAmount = order.TotalAmount,
                AmountPaid = netPaid,
                BalanceAmount = Math.Max(0m, order.TotalAmount - netPaid),
                PaymentStatus = order.PaymentStatus,
                Payments = payments
            };
        }

        private void LoadAttachments(OrderPaymentItem payment)
        {
            payment.AttachmentData = _commonService.GetModuleAttachments(AttachmentModule.OrderPayment, payment.Edid);
        }

        private static List<DocumentAttachmentReq> MapAttachments(List<OrderPaymentAttachmentReq> attachments)
        {
            return attachments.Select(item => new DocumentAttachmentReq
            {
                AttachmentEdid = item.AttachmentEdid,
                AttachmentType = item.AttachmentType.ToString(),
                Remarks = item.Remarks
            }).ToList();
        }

        public OrderPaymentSummaryRes GetPaymentSummary(string orderEdid)
        {
            if (string.IsNullOrWhiteSpace(orderEdid))
                throw new HoneyErpException(ErrorCode.InvalidData, "Order ID is required.");

            int orderId = Convert.ToInt32(EncryptionHelper.Decrypt(orderEdid));
            OrderHeaderSummary order = _paymentRepo.GetOrderHeader(orderId);
            if (order == null)
                throw new HoneyErpException(ErrorCode.NotFound, "Order not found.");

            decimal netPaid = _paymentRepo.ComputeNetPaid(orderId);
            decimal balance = Math.Max(0m, order.TotalAmount - netPaid);
            List<OrderPaymentItem> payments = _paymentRepo.GetPaymentsByOrderId(orderId, orderEdid);

            return new OrderPaymentSummaryRes
            {
                OrderId = orderId,
                OrderEdid = orderEdid,
                TotalAmount = order.TotalAmount,
                AmountPaid = netPaid,
                BalanceAmount = balance,
                PaymentCount = payments.Count,
                PaymentStatus = order.PaymentStatus
            };
        }

        public DeleteOrderPaymentRes DeletePayment(string paymentEdid)
        {
            if (string.IsNullOrWhiteSpace(paymentEdid))
                throw new HoneyErpException(ErrorCode.InvalidData, "Payment ID is required.");

            int paymentId = Convert.ToInt32(EncryptionHelper.Decrypt(paymentEdid));
            OrderPaymentRecord payment = _paymentRepo.GetPaymentById(paymentId);
            if (payment == null)
                throw new HoneyErpException(ErrorCode.NotFound, "Payment record not found.");

            _paymentRepo.DeletePayment(paymentId);

            decimal netPaid = _paymentRepo.ComputeNetPaid(payment.OrderId);
            OrderHeaderSummary order = _paymentRepo.GetOrderHeader(payment.OrderId);
            decimal balance = Math.Max(0m, order.TotalAmount - netPaid);

            PaymentStatus newStatus;
            if (netPaid <= 0)
                newStatus = PaymentStatus.Unpaid;
            else if (netPaid >= order.TotalAmount)
                newStatus = PaymentStatus.Paid;
            else
                newStatus = PaymentStatus.PartiallyPaid;

            _paymentRepo.UpdateOrderPaymentStatus(payment.OrderId, newStatus);

            if (payment.Status == OrderPaymentStatus.Success && _paymentTransactionRepo != null)
            {
                string reversalDesc = $"Order #{payment.OrderNumber} payment void/reversal ({payment.TransactionNumber})";

                _paymentTransactionRepo.Create(new CreatePaymentTransactionReq
                {
                    TransactionNumber = Utilities.GeneratePublicId("TXN", 6),
                    Description = reversalDesc,
                    Amount = payment.Amount,
                    Status = PaymentType.Debit,
                    Category = TransactionCategory.Adjustment,
                    IsActive = true
                });
            }

            return new DeleteOrderPaymentRes
            {
                TotalAmount = order.TotalAmount,
                AmountPaid = netPaid,
                BalanceAmount = balance,
                PaymentStatus = newStatus.ToString(),
            };
        }
    }
}
