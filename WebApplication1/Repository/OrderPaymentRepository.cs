using System;
using System.Collections.Generic;
using System.Data;
using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using Microsoft.Data.SqlClient;

namespace HoneyErpAPI.Repository
{
    public interface IOrderPaymentRepository
    {
        OrderHeaderSummary GetOrderHeader(int orderId);
        int InsertPayment(int orderId, string transactionNumber, RecordOrderPaymentReq req, DateTime paymentDate);
        decimal ComputeNetPaid(int orderId);
        void UpdateOrderPaymentStatus(int orderId, PaymentStatus status);
        List<OrderPaymentItem> GetPaymentsByOrderId(int orderId, string orderEdid);
        OrderPaymentRecord? GetPaymentById(int paymentId);
        void DeletePayment(int paymentId);
    }

    public class OrderPaymentRepository : IOrderPaymentRepository
    {
        private readonly DbClient _client;

        public OrderPaymentRepository(DbClient client)
        {
            _client = client;
        }

        public OrderHeaderSummary GetOrderHeader(int orderId)
        {
            SqlParameter[] p = { new SqlParameter("@OrderId", orderId) };
            DataTable dt = _client.ExecuteAdapter("SELECT id, orderNumber, totalAmount, paymentStatus FROM OrderMaster WHERE id=@OrderId", p, CommandType.Text).Tables[0];
            if (dt.Rows.Count == 0)
                throw new HoneyErpException(ErrorCode.NotFound, "Order not found.");

            DataRow row = dt.Rows[0];
            return new OrderHeaderSummary
            {
                OrderId = orderId,
                OrderNumber = row.Field<string>("orderNumber") ?? "",
                TotalAmount = row.Field<decimal>("totalAmount"),
                PaymentStatus = row.Field<string>("paymentStatus") ?? ""
            };
        }

        public int InsertPayment(int orderId, string transactionNumber, RecordOrderPaymentReq req, DateTime paymentDate)
        {
            int? docId = req.DocId;
            if (!docId.HasValue && !string.IsNullOrWhiteSpace(req.DocEdid))
            {
                string decrypted = EncryptionHelper.Decrypt(req.DocEdid);
                if (int.TryParse(decrypted, out int parsedId))
                    docId = parsedId;
                else if (int.TryParse(req.DocEdid, out int rawId))
                    docId = rawId;
            }

            SqlParameter[] p = new SqlParameter[10];
            p[0] = new SqlParameter("@OrderId", orderId);
            p[1] = new SqlParameter("@TransactionNumber", transactionNumber);
            p[2] = new SqlParameter("@PaymentMethod", req.PaymentMethod.ToString());
            p[3] = new SqlParameter("@PaymentType", req.PaymentType.ToString());
            p[4] = new SqlParameter("@Amount", req.Amount);
            p[5] = new SqlParameter("@Status", req.Status.ToString());
            p[6] = new SqlParameter("@PaymentReference", Utilities.DbValue(req.PaymentReference));
            p[7] = new SqlParameter("@PaymentDate", paymentDate);
            p[8] = new SqlParameter("@Remark", Utilities.DbValue(req.Remark));
            p[9] = new SqlParameter("@DocId", (object?)docId ?? DBNull.Value);

            string insertQuery = @"INSERT INTO OrderPayment (orderId, transactionNumber, paymentMethod, paymentType, amount, status, paymentReference, paymentDate, remark, docId, createdAt)
                                   OUTPUT INSERTED.Id
                                   VALUES (@OrderId, @TransactionNumber, @PaymentMethod, @PaymentType, @Amount, @Status, @PaymentReference, @PaymentDate, @Remark, @DocId, SYSUTCDATETIME())";

            return _client.ExecuteScalar<int>(insertQuery, p, CommandType.Text);
        }

        public decimal ComputeNetPaid(int orderId)
        {
            SqlParameter[] p = {
                new SqlParameter("@OrderId", orderId),
                new SqlParameter("@SuccessStatus", OrderPaymentStatus.Success.ToString()),
                new SqlParameter("@RefundedStatus", OrderPaymentStatus.Refunded.ToString())
            };
            string query = @"SELECT ISNULL(SUM(CASE WHEN status = @SuccessStatus THEN amount WHEN status = @RefundedStatus THEN -amount ELSE 0 END), 0)
                             FROM OrderPayment WHERE orderId = @OrderId";
            return _client.ExecuteScalar<decimal>(query, p, CommandType.Text);
        }

        public void UpdateOrderPaymentStatus(int orderId, PaymentStatus status)
        {
            SqlParameter[] p = {
                new SqlParameter("@OrderId", orderId),
                new SqlParameter("@PaymentStatus", status.ToString())
            };
            _client.ExecuteNonQuery("UPDATE OrderMaster SET paymentStatus = @PaymentStatus WHERE id = @OrderId", p, CommandType.Text);
        }

        public List<OrderPaymentItem> GetPaymentsByOrderId(int orderId, string orderEdid)
        {
            SqlParameter[] p = { new SqlParameter("@OrderId", orderId) };
            string query = @"SELECT id, orderId, transactionNumber, paymentMethod, paymentType, amount, status, paymentReference, paymentDate, remark, docId, createdAt
                             FROM OrderPayment WHERE orderId=@OrderId ORDER BY createdAt DESC";
            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            List<OrderPaymentItem> items = new List<OrderPaymentItem>();
            foreach (DataRow row in dt.Rows)
            {
                int? docId = row.IsNull("docId") ? (int?)null : Convert.ToInt32(row["docId"]);
                string? docEdid = docId.HasValue ? EncryptionHelper.Encrypt(docId.Value.ToString()) : null;

                items.Add(new OrderPaymentItem
                {
                    Edid = EncryptionHelper.Encrypt(row.Field<int>("id").ToString()),
                    OrderEdid = orderEdid,
                    TransactionNumber = row.Field<string?>("transactionNumber") ?? "",
                    PaymentMethod = Utilities.ParseEnum<OrderPaymentMethod>(row.Field<string>("paymentMethod")) ?? OrderPaymentMethod.UPI,
                    PaymentType = Utilities.ParseEnum<OrderPaymentType>(row.Field<string>("paymentType")) ?? OrderPaymentType.FullPayment,
                    Amount = row.Field<decimal>("amount"),
                    Status = Utilities.ParseEnum<OrderPaymentStatus>(row.Field<string>("status")) ?? OrderPaymentStatus.Success,
                    PaymentReference = row.Field<string?>("paymentReference"),
                    PaymentDate = row.Field<DateTime?>("paymentDate"),
                    Remark = row.Field<string?>("remark"),
                    DocId = docId,
                    DocEdid = docEdid,
                    CreatedAt = row.Field<DateTime>("createdAt")
                });
            }

            return items;
        }

        public OrderPaymentRecord? GetPaymentById(int paymentId)
        {
            SqlParameter[] p = { new SqlParameter("@Id", paymentId) };
            string query = @"SELECT p.id, p.orderId, o.orderNumber, p.transactionNumber, p.paymentMethod, p.paymentType, p.amount, p.status, p.paymentReference, p.paymentDate, p.remark, p.docId, p.createdAt
                             FROM OrderPayment p
                             LEFT JOIN OrderMaster o ON p.orderId = o.id
                             WHERE p.id=@Id";
            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];
            if (dt.Rows.Count == 0)
                return null;

            DataRow row = dt.Rows[0];
            int? docId = row.IsNull("docId") ? (int?)null : Convert.ToInt32(row["docId"]);
            string? docEdid = docId.HasValue ? EncryptionHelper.Encrypt(docId.Value.ToString()) : null;

            return new OrderPaymentRecord
            {
                Id = row.Field<int>("id"),
                OrderId = row.Field<int>("orderId"),
                OrderNumber = row.Field<string?>("orderNumber") ?? "",
                TransactionNumber = row.Field<string?>("transactionNumber") ?? "",
                PaymentMethod = Utilities.ParseEnum<OrderPaymentMethod>(row.Field<string>("paymentMethod")) ?? OrderPaymentMethod.UPI,
                PaymentType = Utilities.ParseEnum<OrderPaymentType>(row.Field<string>("paymentType")) ?? OrderPaymentType.FullPayment,
                Amount = row.Field<decimal>("amount"),
                Status = Utilities.ParseEnum<OrderPaymentStatus>(row.Field<string>("status")) ?? OrderPaymentStatus.Success,
                PaymentReference = row.Field<string?>("paymentReference"),
                PaymentDate = row.Field<DateTime?>("paymentDate"),
                Remark = row.Field<string?>("remark"),
                DocId = docId,
                DocEdid = docEdid,
                CreatedAt = row.Field<DateTime>("createdAt")
            };
        }

        public void DeletePayment(int paymentId)
        {
            SqlParameter[] p = { new SqlParameter("@Id", paymentId) };
            _client.ExecuteNonQuery("DELETE FROM OrderPayment WHERE id=@Id", p, CommandType.Text);
        }
    }
}
