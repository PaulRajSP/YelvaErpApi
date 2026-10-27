using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Linq;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Repository
{
    public interface IOrderRepository
    {
        PageResult<OrderListItem>  GetAll(GetOrderListReq req);
        GetOrderByIdRes? GetById(string edid);
        CreateOrderRes Create(CreateOrderReq req, string? createdBy = null);
        bool UpdateStatus(string edid, OrderStatus status);
        bool UpdateStatus(string edid, OrderStatus status, string? remarks = null, string? changedBy = null);
        GetOrderStatusTrackingRes GetOrderStatusTracking(string orderNumber, string? orderEdid = null);
        bool UpdatePaymentStatus(string edid, OrderStatus paymentStatus);
        GetOrderItemsRes GetItems(string orderEdid);
        GetOrderCommentsRes  GetComments(string orderEdid);
        string AddComment(string orderEdid, AddCommentReq req);
        GetPackageRes? GetPackage(string orderEdid);
        SavePackageRes SavePackage(string orderEdid, SavePackageReq req);
        GetOrderFullDetailsRes? GetOrderFullDetails(string orderNumber, string? orderEdid = null);

        bool ConfirmOrder(string edid, string? changedBy = null);
        List<OrderProductItemDto> GetOrderProductItems(int orderId);
        GetOrderBottlesRes GetOrderBottles(string orderNumber);
        GetOrderPackingDetailsRes? GetOrderPackingDetails(string orderNumber);
        bool ConfirmPacking(ConfirmPackingReq req);
        bool DispatchOrder(DispatchOrderReq req);
        PageResult<OrderListItem> GetReadyToShipOrders(GetOrderListReq req);
        List<string> GetOrderNumbersByStatus(string status);
    }

    public class OrderRepository : IOrderRepository
    {
        private readonly DbClient _client;
        public OrderRepository(DbClient client) => _client = client;

        // ── Order ─────────────────────────────────────────────────────────────

        public PageResult<OrderListItem> GetAll(GetOrderListReq req)
        {
            int pageNumber = req.Pagination.PageNumber > 0 ? req.Pagination.PageNumber : 1;
            int pageSize   = req.Pagination.PageSize   > 0 ? req.Pagination.PageSize   : 10;
            int offset     = (pageNumber - 1) * pageSize;

            string query = @"SELECT COUNT(*) OVER() AS TotalRecords, o.id, o.orderNumber, o.customerId, c.name AS customerName, c.mobile AS customerMobile,
                             o.deliveryType, a.recipientName, o.subtotal, o.deliveryChargeGiven AS deliveryCharge,
                             o.discount, o.totalAmount, 
                             ISNULL(p.amountPaid, 0) AS amountPaid,
                             (o.totalAmount - ISNULL(p.amountPaid, 0)) AS balanceAmount,
                             CASE
                                 WHEN ISNULL(p.amountPaid, 0) <= 0 THEN 'Unpaid'
                                 WHEN ISNULL(p.amountPaid, 0) < o.totalAmount THEN 'PartiallyPaid'
                                 ELSE 'Paid'
                             END AS paymentStatus,
                             o.status, o.createdAt
                             FROM OrderMaster o
                             LEFT JOIN CustomerMaster c ON o.customerId = c.id
                             LEFT JOIN AddressMaster a ON o.deliveryAddressId = a.id
                             OUTER APPLY
                             (
                                 SELECT SUM(CASE WHEN op.status = 'Success' THEN op.amount WHEN op.status = 'Refunded' THEN -op.amount ELSE 0 END) AS amountPaid
                                 FROM OrderPayment op
                                 WHERE op.orderId = o.id
                             ) p
                             WHERE (@FromDate IS NULL OR CAST(o.createdAt AS DATE) >= CAST(@FromDate AS DATE))
                               AND (@ToDate IS NULL OR CAST(o.createdAt AS DATE) <= CAST(@ToDate AS DATE))
                               AND (@Status IS NULL OR o.status = @Status)
                               AND (@PaymentStatus IS NULL OR o.paymentStatus = @PaymentStatus OR
                                    (@PaymentStatus = 'UNPAID' AND (o.paymentStatus = 'UNPAID' OR o.paymentStatus = 'Unpaid' OR ISNULL(p.amountPaid, 0) <= 0)) OR
                                    (@PaymentStatus = 'PAID' AND (o.paymentStatus = 'PAID' OR o.paymentStatus = 'Paid' OR ISNULL(p.amountPaid, 0) >= o.totalAmount)) OR
                                    (@PaymentStatus = 'PARTIALLY_PAID' AND (o.paymentStatus = 'PARTIALLY_PAID' OR (ISNULL(p.amountPaid, 0) > 0 AND ISNULL(p.amountPaid, 0) < o.totalAmount))))
                               AND (@SearchKeyword IS NULL OR o.orderNumber LIKE '%' + @SearchKeyword + '%' OR c.name LIKE '%' + @SearchKeyword + '%' OR c.mobile LIKE '%' + @SearchKeyword + '%')
                             ORDER BY o.createdAt DESC
                             OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            SqlParameter[] p = new SqlParameter[7];
            p[0] = new SqlParameter("@Offset", offset);
            p[1] = new SqlParameter("@PageSize", pageSize);
            p[2] = new SqlParameter("@FromDate", Utilities.DbValue(req.FromDate));
            p[3] = new SqlParameter("@ToDate", Utilities.DbValue(req.ToDate));
            p[4] = new SqlParameter("@Status", Utilities.DbValue(req.Status));
            p[5] = new SqlParameter("@PaymentStatus", Utilities.DbValue(req.PaymentStatus));
            p[6] = new SqlParameter("@SearchKeyword", Utilities.DbValue(req.SearchKeyword));

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            int totalCount = dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["TotalRecords"]) : 0;

            PageResult<OrderListItem> res = new PageResult<OrderListItem>
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize   = pageSize
            };

            foreach (DataRow row in dt.Rows)
            {
                decimal rowTotal = row.Field<decimal>("totalAmount");
                decimal rowPaid = row.IsNull("amountPaid") ? 0m : Convert.ToDecimal(row["amountPaid"]);
                decimal rowBal = row.IsNull("balanceAmount") ? Math.Max(0m, rowTotal - rowPaid) : Convert.ToDecimal(row["balanceAmount"]);

                res.Items.Add(new OrderListItem
                {
                    Edid           = EncryptionHelper.Encrypt(row.Field<int>("id").ToString()),
                    OrderNumber    = row.Field<string>("orderNumber")!,
                    customerEdid   = EncryptionHelper.Encrypt(row.Field<int>("customerId").ToString()),
                    CustomerName   = row.Field<string?>("customerName"),
                    CustomerMobile = row.Field<string?>("customerMobile"),
                    RecipientName  = row.Field<string?>("recipientName"),
                    DeliveryType   = row.Field<string?>("deliveryType"),
                    TotalAmount    = rowTotal,
                    AmountPaid     = rowPaid,
                    BalanceAmount  = rowBal,
                    PaymentStatus  = row.Field<string>("paymentStatus")!,
                    Status         = row.Field<string>("status")!,
                    CreatedAt      = row.Field<DateTime>("createdAt")
                });
            }

            return res;
        }

        public GetOrderByIdRes? GetById(string edid)
        {
            SqlParameter[] p = { new SqlParameter("@Id", EncryptionHelper.Decrypt(edid)) };

            string query = @"SELECT o.id,o.orderNumber,o.customerId,o.deliveryType,o.deliveryAddressId,o.subtotal,o.deliveryChargeGiven AS deliveryCharge,o.discount,o.totalAmount,
                             ISNULL(p.amountPaid, 0) AS amountPaid,
                             (o.totalAmount - ISNULL(p.amountPaid, 0)) AS balanceAmount,
                             CASE
                                 WHEN ISNULL(p.amountPaid, 0) <= 0 THEN 'Unpaid'
                                 WHEN ISNULL(p.amountPaid, 0) < o.totalAmount THEN 'PartiallyPaid'
                                 ELSE 'Paid'
                             END AS paymentStatus,
                             o.status,o.createdAt
                             FROM OrderMaster o
                             OUTER APPLY
                             (
                                 SELECT SUM(CASE WHEN op.status = 'Success' THEN op.amount WHEN op.status = 'Refunded' THEN -op.amount ELSE 0 END) AS amountPaid
                                 FROM OrderPayment op
                                 WHERE op.orderId = o.id
                             ) p
                             WHERE o.id=@Id";
            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            int? deliveryAddressId = row.IsNull("deliveryAddressId") ? null : row.Field<int>("deliveryAddressId");
            decimal totalAmount = row.Field<decimal>("totalAmount");
            decimal amountPaid = row.IsNull("amountPaid") ? 0m : Convert.ToDecimal(row["amountPaid"]);
            decimal balanceAmount = row.IsNull("balanceAmount") ? Math.Max(0m, totalAmount - amountPaid) : Convert.ToDecimal(row["balanceAmount"]);

            return new GetOrderByIdRes
            {
                Edid                  = EncryptionHelper.Encrypt(row.Field<int>("id").ToString()),
                OrderNumber           = row.Field<string>("orderNumber")!,
                customerEdid          = EncryptionHelper.Encrypt(row.Field<int>("customerId").ToString()),
                DeliveryType          = row.Field<string?>("deliveryType"),
                DeliveryAddressEdid   = deliveryAddressId.HasValue ? EncryptionHelper.Encrypt(deliveryAddressId.Value.ToString()) : null,
                Subtotal              = row.Field<decimal>("subtotal"),
                DeliveryCharge        = row.Field<decimal>("deliveryCharge"),
                Discount              = row.Field<decimal>("discount"),
                TotalAmount           = totalAmount,
                AmountPaid            = amountPaid,
                BalanceAmount         = balanceAmount,
                PaymentStatus         = row.Field<string>("paymentStatus")!,
                Status                = row.Field<string>("status")!,
                CreatedAt             = row.Field<DateTime>("createdAt")
            };
        }

        public CreateOrderRes Create(CreateOrderReq req, string? createdBy = null)
        {
            string customerId = EncryptionHelper.Decrypt(req.Order.customerEdid);
            string? deliveryAddressId = !string.IsNullOrWhiteSpace(req.Order.DeliveryAddressEdid)
                ? EncryptionHelper.Decrypt(req.Order.DeliveryAddressEdid) : null;

            string orderNumber = Utilities.GeneratePublicId("ORD",5);

            decimal initialPaid = req.Order.InitialPaymentAmount ?? 0m;
            PaymentStatus initialPaymentStatus = PaymentStatus.Unpaid;
            if (initialPaid > 0)
            {
                initialPaymentStatus = initialPaid >= req.Order.TotalAmount ? PaymentStatus.Paid : PaymentStatus.PartiallyPaid;
            }

            SqlParameter[] p = new SqlParameter[12];
            p[0] = new SqlParameter("@OrderNumber",       orderNumber);
            p[1] = new SqlParameter("@customerId",        customerId);
            p[2] = new SqlParameter("@DeliveryType",      Utilities.DbValue(req.Order.DeliveryType));
            p[3] = new SqlParameter("@DeliveryAddressId", Utilities.DbValue(deliveryAddressId));
            p[4] = new SqlParameter("@Subtotal",          req.Order.Subtotal);
            p[5] = new SqlParameter("@DeliveryCharge",    req.Order.DeliveryCharge);
            p[6] = new SqlParameter("@Discount",          req.Order.Discount);
            p[7] = new SqlParameter("@TotalAmount",       req.Order.TotalAmount);
            p[8] = new SqlParameter("@PaymentStatus",     initialPaymentStatus.ToString());
            p[9] = new SqlParameter("@Status",            OrderStatus.OrderPlaced.ToString());
            p[10] = new SqlParameter("@CreatedAt",        DateTime.UtcNow);
            p[11] = new SqlParameter("@CreatedBy",        Utilities.DbValue(createdBy));

            string insertQuery = @"INSERT INTO OrderMaster (orderNumber,customerId,deliveryType,deliveryAddressId,subtotal,deliveryChargeGiven,discount,totalAmount,paymentStatus,status,createdAt,createdBy)
                  OUTPUT INSERTED.Id VALUES (@OrderNumber,@customerId,@DeliveryType,@DeliveryAddressId,@Subtotal,@DeliveryCharge,@Discount,@TotalAmount,@PaymentStatus,@Status,@CreatedAt,@CreatedBy)";
            int orderId = _client.ExecuteScalar<int>(insertQuery, p, CommandType.Text);

            LogStatusTracking(orderId, OrderStatus.OrderPlaced.ToString(), "Order placed successfully", createdBy ?? "EMP001", DateTime.UtcNow);

            if (initialPaid > 0)
            {
                string txnNumber = Utilities.GeneratePublicId("TXN", 6);
                string payMethod = !string.IsNullOrWhiteSpace(req.Order.InitialPaymentMethod) ? req.Order.InitialPaymentMethod : OrderPaymentMethod.UPI.ToString();
                string payType = !string.IsNullOrWhiteSpace(req.Order.InitialPaymentType) ? req.Order.InitialPaymentType : (initialPaid >= req.Order.TotalAmount ? OrderPaymentType.FullPayment.ToString() : OrderPaymentType.Advance.ToString());
                string payStatus = OrderPaymentStatus.Success.ToString();

                int? initialDocId = req.Order.InitialPaymentDocId;
                if (!initialDocId.HasValue && !string.IsNullOrWhiteSpace(req.Order.InitialPaymentDocEdid))
                {
                    string decrypted = EncryptionHelper.Decrypt(req.Order.InitialPaymentDocEdid);
                    if (int.TryParse(decrypted, out int parsedId))
                        initialDocId = parsedId;
                    else if (int.TryParse(req.Order.InitialPaymentDocEdid, out int rawId))
                        initialDocId = rawId;
                }

                SqlParameter[] payP = new SqlParameter[10];
                payP[0] = new SqlParameter("@OrderId", orderId);
                payP[1] = new SqlParameter("@TransactionNumber", txnNumber);
                payP[2] = new SqlParameter("@PaymentMethod", payMethod);
                payP[3] = new SqlParameter("@PaymentType", payType);
                payP[4] = new SqlParameter("@Amount", initialPaid);
                payP[5] = new SqlParameter("@Status", payStatus);
                payP[6] = new SqlParameter("@PaymentReference", Utilities.DbValue(req.Order.InitialPaymentReference));
                payP[7] = new SqlParameter("@PaymentDate", DateTime.UtcNow);
                payP[8] = new SqlParameter("@Remark", "Initial payment on order creation");
                payP[9] = new SqlParameter("@DocId", (object?)initialDocId ?? DBNull.Value);

                string payQuery = @"INSERT INTO OrderPayment (orderId, transactionNumber, paymentMethod, paymentType, amount, status, paymentReference, paymentDate, remark, docId, createdAt)
                                    VALUES (@OrderId, @TransactionNumber, @PaymentMethod, @PaymentType, @Amount, @Status, @PaymentReference, @PaymentDate, @Remark, @DocId, SYSUTCDATETIME())";
                _client.ExecuteNonQuery(payQuery, payP, CommandType.Text);

                // Entry in TransactionMaster & PaymentTransactionMaster
                string txnDesc = $"Order #{orderNumber} payment via {payMethod}";
                if (!string.IsNullOrWhiteSpace(req.Order.InitialPaymentReference))
                    txnDesc += $" (Ref: {req.Order.InitialPaymentReference})";

                try
                {
                    SqlParameter[] lp = new SqlParameter[6];
                    lp[0] = new SqlParameter("@TransactionNumber", txnNumber);
                    lp[1] = new SqlParameter("@Description",       txnDesc);
                    lp[2] = new SqlParameter("@Amount",            initialPaid);
                    lp[3] = new SqlParameter("@Status",            PaymentType.Credit.ToString());
                    lp[4] = new SqlParameter("@Category",          TransactionCategory.Sale.ToString());
                    lp[5] = new SqlParameter("@IsActive",          true);

                    string ledgerQuery = @"
                        DECLARE @PrevBalance DECIMAL(18,2) = 0;
                        SELECT TOP 1 @PrevBalance = ISNULL(balance, 0) FROM PaymentTransactionMaster ORDER BY id DESC;
                        DECLARE @NewBalance DECIMAL(18,2) = @PrevBalance + @Amount;

                        INSERT INTO PaymentTransactionMaster (transactionNumber, remark, amount, status, category, isActive, balance, createdAt) 
                        VALUES (@TransactionNumber, @Description, @Amount, @Status, @Category, @IsActive, @NewBalance, SYSUTCDATETIME());";

                    _client.ExecuteNonQuery(ledgerQuery, lp, CommandType.Text);
                }
                catch { }
            }

            foreach (CreateOrderLine item in req.Items)
            {
                string productId = EncryptionHelper.Decrypt(item.ProductEdid);
                string productPricingId = EncryptionHelper.Decrypt(item.PriceItemEdid);

                SqlParameter[] ip = new SqlParameter[7];
                ip[0] = new SqlParameter("@OrderId", orderId);
                ip[1] = new SqlParameter("@ProductId", productId);
                ip[2] = new SqlParameter("@ProductNameSnapshot", Utilities.DbValue(item.ProductNameSnapshot));
                ip[3] = new SqlParameter("@UnitPrice", item.UnitPrice);
                ip[4] = new SqlParameter("@Quantity", item.Quantity);
                ip[5] = new SqlParameter("@IsCombo", item.IsCombo);
                ip[6] = new SqlParameter("@ProductPricingId", productPricingId);

                _client.ExecuteNonQuery(
                    "INSERT INTO OrderItem (orderId,productId,productNameSnapshot,unitPrice,quantity,isCombo,productPricingId) VALUES (@OrderId,@ProductId,@ProductNameSnapshot,@UnitPrice,@Quantity,@IsCombo,@ProductPricingId)",
                    ip, CommandType.Text);

                SqlParameter[] opmP = new SqlParameter[4];
                opmP[0] = new SqlParameter("@OrderId", orderId);
                opmP[1] = new SqlParameter("@ProductId", productId);
                opmP[2] = new SqlParameter("@Quantity", item.Quantity);
                opmP[3] = new SqlParameter("@ProductPricingId", productPricingId);

                _client.ExecuteNonQuery(
                    "INSERT INTO OrderProductMaster (orderId, productId, quantity, productPricingId) VALUES (@OrderId, @ProductId, @Quantity, @ProductPricingId)",
                    opmP, CommandType.Text);
            }

            CreateOrderRes resp = new CreateOrderRes();
            resp.Edid = EncryptionHelper.Encrypt(orderId.ToString());
            resp.OrderNumber = orderNumber;
            return resp;
        }

        public bool UpdateStatus(string edid, OrderStatus status)
        {
            return UpdateStatus(edid, status, null, null);
        }

        public bool UpdateStatus(string edid, OrderStatus status, string? remarks = null, string? changedBy = null)
        {
            int orderId = int.Parse(EncryptionHelper.Decrypt(edid));

            SqlParameter[] checkP = { new SqlParameter("@Id", orderId) };
            DataTable dtOrder = _client.ExecuteAdapter("SELECT status, orderNumber FROM OrderMaster WHERE id = @Id", checkP, CommandType.Text).Tables[0];
            if (dtOrder.Rows.Count == 0)
                throw new HoneyErpException(ErrorCode.NotFound, "Order not found");

            string currentStatus = dtOrder.Rows[0].Field<string>("status") ?? "";

            if (status == OrderStatus.Cancelled)
            {
                if (string.Equals(currentStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // Strictly allow cancellation ONLY for: OrderPlaced, Confirmed, Packing, ReadyToShip
                var allowedForCancel = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    OrderStatus.OrderPlaced.ToString(),
                    OrderStatus.Confirmed.ToString(),
                    OrderStatus.Packing.ToString(),
                    OrderStatus.ReadyToShip.ToString()
                };

                string normCurrent = (currentStatus ?? "").Replace(" ", "").Replace("_", "");
                if (!allowedForCancel.Contains(normCurrent))
                {
                    throw new HoneyErpException(ErrorCode.InvalidData, 
                        $"Cannot cancel order with status '{currentStatus}'. Cancellation is only allowed for orders with status 'Order Placed', 'Confirmed', 'Packing', or 'Ready to Ship'.");
                }

                // 1. Restore product stock in ProductBatchMaster for any packed / batch-assigned bottles
                SqlParameter[] assignedP = { new SqlParameter("@OrderId", orderId) };
                string assignedSql = @"
                    SELECT opi.productBatchId, COUNT(1) AS qty
                    FROM OrderProductItem opi
                    INNER JOIN OrderProductMaster opm ON opi.orderProductMstId = opm.id
                    WHERE opm.orderId = @OrderId 
                      AND opi.productBatchId IS NOT NULL 
                      AND opi.status != 'Cancelled'
                    GROUP BY opi.productBatchId";
                DataTable assignedDt = _client.ExecuteAdapter(assignedSql, assignedP, CommandType.Text).Tables[0];

                foreach (DataRow r in assignedDt.Rows)
                {
                    int batchId = r.Field<int>("productBatchId");
                    int qty = r.Field<int>("qty");
                    if (qty > 0)
                    {
                        SqlParameter[] resP = {
                            new SqlParameter("@BatchId", batchId),
                            new SqlParameter("@Qty", (decimal)qty)
                        };
                        string restoreBatchSql = @"UPDATE ProductBatchMaster 
                                                   SET remainingQty = remainingQty + @Qty 
                                                   WHERE id = @BatchId";
                        _client.ExecuteNonQuery(restoreBatchSql, resP, CommandType.Text);
                    }
                }

                // 2. Mark physical bottle items as Cancelled
                SqlParameter[] opiCancelP = { new SqlParameter("@OrderId", orderId) };
                string cancelOpiSql = @"
                    UPDATE opi
                    SET opi.status = 'Cancelled'
                    FROM OrderProductItem opi
                    INNER JOIN OrderProductMaster opm ON opi.orderProductMstId = opm.id
                    WHERE opm.orderId = @OrderId";
                _client.ExecuteNonQuery(cancelOpiSql, opiCancelP, CommandType.Text);

                // 3. Mark packaging as Cancelled (do not refund packing material stock, but cancel package record so it is removed from prints)
                SqlParameter[] pkgCancelP = { new SqlParameter("@OrderId", orderId) };
                string cancelPkgSql = "UPDATE OrderPackage SET status = 'Cancelled' WHERE orderId = @OrderId";
                _client.ExecuteNonQuery(cancelPkgSql, pkgCancelP, CommandType.Text);
            }
            else if (status == OrderStatus.Confirmed)
            {
                CreatePhysicalBottlesIfNotExist(orderId);
            }

            SqlParameter[] p = new SqlParameter[2];
            p[0] = new SqlParameter("@Id",     orderId);
            p[1] = new SqlParameter("@Status", status.ToString());
            bool success = _client.ExecuteNonQuery("UPDATE OrderMaster SET status=@Status WHERE id=@Id", p, CommandType.Text) > 0;
            if (success)
            {
                string effectiveRemarks = !string.IsNullOrWhiteSpace(remarks)
                    ? remarks
                    : (status == OrderStatus.Confirmed ? "Order confirmed and physical bottles generated" : 
                       status == OrderStatus.Cancelled ? "Order cancelled by user" : $"Status updated to {status}");
                LogStatusTracking(orderId, status.ToString(), effectiveRemarks, changedBy ?? "EMP001", DateTime.UtcNow);
            }
            return success;
        }

        public bool ConfirmOrder(string edid, string? changedBy = null)
        {
            return UpdateStatus(edid, OrderStatus.Confirmed, "Order confirmed and physical bottles generated", changedBy ?? "EMP001");
        }

        public void LogStatusTracking(int orderId, string status, string? remarks = null, string? changedBy = null, DateTime? changedAt = null)
        {
            try
            {
                SqlParameter[] p = new SqlParameter[5];
                p[0] = new SqlParameter("@OrderId", orderId);
                p[1] = new SqlParameter("@Status", status);
                p[2] = new SqlParameter("@Remarks", Utilities.DbValue(remarks));
                p[3] = new SqlParameter("@ChangedBy", Utilities.DbValue(changedBy ?? "EMP001"));
                p[4] = new SqlParameter("@ChangedAt", changedAt ?? DateTime.UtcNow);

                string sql = @"INSERT INTO OrderStatusTracking (orderId, status, remarks, changedBy, changedAt)
                               VALUES (@OrderId, @Status, @Remarks, @ChangedBy, @ChangedAt)";
                _client.ExecuteNonQuery(sql, p, CommandType.Text);
            }
            catch
            {
                // Status tracking failure should not abort main transaction
            }
        }

        public GetOrderStatusTrackingRes GetOrderStatusTracking(string orderNumber, string? orderEdid = null)
        {
            int? resolvedOrderId = null;
            if (!string.IsNullOrWhiteSpace(orderEdid))
            {
                try
                {
                    if (int.TryParse(EncryptionHelper.Decrypt(orderEdid), out int idVal))
                        resolvedOrderId = idVal;
                    else if (int.TryParse(orderEdid, out int rawVal))
                        resolvedOrderId = rawVal;
                }
                catch { }
            }

            if (resolvedOrderId == null && !string.IsNullOrWhiteSpace(orderNumber))
            {
                if (orderNumber.StartsWith("ORD-", StringComparison.OrdinalIgnoreCase))
                {
                    SqlParameter[] np = { new SqlParameter("@OrderNumber", orderNumber.Trim()) };
                    var idObj = _client.ExecuteScalar<object>("SELECT id FROM OrderMaster WHERE orderNumber = @OrderNumber", np, CommandType.Text);
                    if (idObj != null && int.TryParse(idObj.ToString(), out int oId))
                        resolvedOrderId = oId;
                }
                else
                {
                    try
                    {
                        if (int.TryParse(EncryptionHelper.Decrypt(orderNumber), out int decryptedId))
                            resolvedOrderId = decryptedId;
                    }
                    catch { }

                    if (resolvedOrderId == null)
                    {
                        SqlParameter[] np = { new SqlParameter("@OrderNumber", orderNumber.Trim()) };
                        var idObj = _client.ExecuteScalar<object>("SELECT id FROM OrderMaster WHERE orderNumber = @OrderNumber", np, CommandType.Text);
                        if (idObj != null && int.TryParse(idObj.ToString(), out int oId))
                            resolvedOrderId = oId;
                    }
                }
            }

            if (resolvedOrderId == null)
            {
                throw new HoneyErpException(ErrorCode.NotFound, "Order not found");
            }

            SqlParameter[] op = { new SqlParameter("@OrderId", resolvedOrderId.Value) };
            string orderInfoSql = @"SELECT o.orderNumber, o.status, o.createdAt, o.createdBy,
                                           COALESCE(ead.employeeName, o.createdBy) AS creatorName
                                    FROM OrderMaster o
                                    LEFT JOIN EmployeeMaster em ON em.employeeCode = o.createdBy
                                    LEFT JOIN EmployeeAdditionalDetails ead ON ead.employeeMstId = em.employeeId
                                    WHERE o.id = @OrderId";
            DataTable oDt = _client.ExecuteAdapter(orderInfoSql, op, CommandType.Text).Tables[0];
            if (oDt.Rows.Count == 0)
            {
                throw new HoneyErpException(ErrorCode.NotFound, "Order not found");
            }

            string actualOrderNumber = oDt.Rows[0].Field<string>("orderNumber") ?? orderNumber;
            string currentStatus = oDt.Rows[0].Field<string>("status") ?? string.Empty;
            DateTime orderCreatedAt = oDt.Rows[0].Field<DateTime>("createdAt");
            string orderCreatedBy = oDt.Rows[0].Field<string?>("createdBy") ?? "EMP001";
            string orderCreatorName = oDt.Rows[0].Field<string?>("creatorName") ?? orderCreatedBy;

            GetOrderStatusTrackingRes res = new GetOrderStatusTrackingRes
            {
                OrderEdid = EncryptionHelper.Encrypt(resolvedOrderId.Value.ToString()),
                OrderNumber = actualOrderNumber,
                CurrentStatus = currentStatus,
                Items = new List<OrderStatusTrackingDto>()
            };

            SqlParameter[] tp = { new SqlParameter("@OrderId", resolvedOrderId.Value) };
            string trackingSql = @"SELECT ost.id, ost.status, ost.remarks, 
                                          ost.changedBy AS employeeCode,
                                          COALESCE(ead.employeeName, ost.changedBy) AS employeeName, 
                                          ost.changedAt 
                                   FROM OrderStatusTracking ost
                                   LEFT JOIN EmployeeMaster em ON em.employeeCode = ost.changedBy
                                   LEFT JOIN EmployeeAdditionalDetails ead ON ead.employeeMstId = em.employeeId
                                   WHERE ost.orderId = @OrderId 
                                   ORDER BY ost.changedAt ASC, ost.id ASC";
            DataTable tDt = _client.ExecuteAdapter(trackingSql, tp, CommandType.Text).Tables[0];

            if (tDt.Rows.Count == 0)
            {
                res.Items.Add(new OrderStatusTrackingDto
                {
                    Edid = EncryptionHelper.Encrypt(resolvedOrderId.Value.ToString()),
                    Status = OrderStatus.OrderPlaced.ToString(),
                    Remarks = "Order placed successfully",
                    ChangedBy = orderCreatedBy,
                    EmployeeName = orderCreatorName,
                    ChangedAt = orderCreatedAt
                });
            }
            else
            {
                foreach (DataRow row in tDt.Rows)
                {
                    res.Items.Add(new OrderStatusTrackingDto
                    {
                        Edid = EncryptionHelper.Encrypt(row.Field<int>("id").ToString()),
                        Status = row.Field<string>("status") ?? string.Empty,
                        Remarks = row.Field<string?>("remarks"),
                        ChangedBy = row.Field<string?>("employeeCode"),
                        EmployeeName = row.Field<string?>("employeeName"),
                        ChangedAt = row.Field<DateTime>("changedAt")
                    });
                }
            }

            return res;
        }

        private void CreatePhysicalBottlesIfNotExist(int orderId)
        {
            SqlParameter[] syncP = { new SqlParameter("@OrderId", orderId) };
            string syncSql = @"INSERT INTO OrderProductMaster (orderId, productId, quantity, productPricingId)
                               SELECT oi.orderId, oi.productId, oi.quantity, oi.productPricingId 
                               FROM OrderItem oi 
                               WHERE oi.orderId = @OrderId 
                                 AND NOT EXISTS (
                                     SELECT 1 FROM OrderProductMaster opm 
                                     WHERE opm.orderId = oi.orderId AND opm.productId = oi.productId
                                 )";
            _client.ExecuteNonQuery(syncSql, syncP, CommandType.Text);

            SqlParameter[] checkP = { new SqlParameter("@OrderId", orderId) };
            string checkSql = @"SELECT COUNT(1) FROM OrderProductItem opi 
                                JOIN OrderProductMaster opm ON opi.orderProductMstId = opm.id 
                                WHERE opm.orderId = @OrderId";
            int existingCount = _client.ExecuteScalar<int>(checkSql, checkP, CommandType.Text);
            if (existingCount > 0) return;

            SqlParameter[] itemsP = { new SqlParameter("@OrderId", orderId) };
            string itemsSql = @"SELECT opm.id AS orderProductMstId, oi.productId, oi.quantity, oi.isCombo 
                                FROM OrderItem oi 
                                JOIN OrderProductMaster opm ON oi.orderId = opm.orderId AND oi.productId = opm.productId 
                                WHERE oi.orderId = @OrderId";
            DataTable itemsDt = _client.ExecuteAdapter(itemsSql, itemsP, CommandType.Text).Tables[0];

            foreach (DataRow itemRow in itemsDt.Rows)
            {
                int orderProductMstId = itemRow.Field<int>("orderProductMstId");
                int productId = itemRow.Field<int>("productId");
                int quantity = itemRow.Field<int>("quantity");
                bool isCombo = !itemRow.IsNull("isCombo") && Convert.ToBoolean(itemRow["isCombo"]);

                if (isCombo)
                {
                    SqlParameter[] comboP = { new SqlParameter("@ComboProductId", productId) };
                    string comboSql = "SELECT productId, quantity FROM ProductComboItem WHERE comboProductId = @ComboProductId";
                    DataTable comboDt = _client.ExecuteAdapter(comboSql, comboP, CommandType.Text).Tables[0];

                    foreach (DataRow cRow in comboDt.Rows)
                    {
                        int childProductId = cRow.Field<int>("productId");
                        decimal childQty = cRow.IsNull("quantity") ? 1m : Convert.ToDecimal(cRow["quantity"]);
                        int totalBottles = quantity * (int)Math.Max(1, childQty);

                        for (int b = 0; b < totalBottles; b++)
                        {
                            SqlParameter[] insP = new SqlParameter[3];
                            insP[0] = new SqlParameter("@OrderProductMstId", orderProductMstId);
                            insP[1] = new SqlParameter("@ProductSingleId", childProductId);
                            insP[2] = new SqlParameter("@Status", OrderProductItemStatus.Pending.ToString());

                            string insSql = @"INSERT INTO OrderProductItem (orderProductMstId, productSingleId, productBatchId, status, packedBy, packedAt)
                                              VALUES (@OrderProductMstId, @ProductSingleId, NULL, @Status, NULL, NULL)";
                            _client.ExecuteNonQuery(insSql, insP, CommandType.Text);
                        }
                    }
                }
                else
                {
                    for (int b = 0; b < quantity; b++)
                    {
                        SqlParameter[] insP = new SqlParameter[3];
                        insP[0] = new SqlParameter("@OrderProductMstId", orderProductMstId);
                        insP[1] = new SqlParameter("@ProductSingleId", productId);
                        insP[2] = new SqlParameter("@Status", OrderProductItemStatus.Pending.ToString());

                        string insSql = @"INSERT INTO OrderProductItem (orderProductMstId, productSingleId, productBatchId, status, packedBy, packedAt)
                                          VALUES (@OrderProductMstId, @ProductSingleId, NULL, @Status, NULL, NULL)";
                        _client.ExecuteNonQuery(insSql, insP, CommandType.Text);
                    }
                }
            }
        }

        public bool UpdatePaymentStatus(string edid, OrderStatus paymentStatus)
        {
            SqlParameter[] p = new SqlParameter[2];
            p[0] = new SqlParameter("@Id", EncryptionHelper.Decrypt(edid));
            p[1] = new SqlParameter("@PaymentStatus", paymentStatus.ToString());
            return _client.ExecuteNonQuery("UPDATE OrderMaster SET paymentStatus=@PaymentStatus WHERE id=@Id", p, CommandType.Text) > 0;
        }

        // ── Order Items ───────────────────────────────────────────────────────

        public GetOrderItemsRes GetItems(string orderEdid)
        {
            string orderId = EncryptionHelper.Decrypt(orderEdid);
            SqlParameter[] p = { new SqlParameter("@OrderId", orderId) };

            string    query = "SELECT productId,productNameSnapshot,unitPrice,quantity,isCombo FROM OrderItem WHERE orderId=@OrderId";
            DataTable dt    = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            GetOrderItemsRes res = new GetOrderItemsRes();
            foreach (DataRow row in dt.Rows)
            {
                res.Items.Add(new OrderItemLine
                {
                    ProductEdid         = EncryptionHelper.Encrypt(row.Field<int>("productId").ToString()),
                    ProductNameSnapshot = row.Field<string?>("productNameSnapshot"),
                    UnitPrice           = row.Field<decimal>("unitPrice"),
                    Quantity            = row.Field<int>("quantity"),
                    IsCombo             = row.Field<bool>("isCombo")
                });
            }
            return res;
        }

        // ── Comments ──────────────────────────────────────────────────────────

        private int ResolveOrderId(string orderEdidOrNumber)
        {
            if (string.IsNullOrWhiteSpace(orderEdidOrNumber))
                throw new HoneyErpException(ErrorCode.NotFound, "Order not found");

            if (orderEdidOrNumber.StartsWith("ORD-", StringComparison.OrdinalIgnoreCase))
            {
                SqlParameter[] np = { new SqlParameter("@OrderNumber", orderEdidOrNumber.Trim()) };
                var idObj = _client.ExecuteScalar<object>("SELECT id FROM OrderMaster WHERE orderNumber = @OrderNumber", np, CommandType.Text);
                if (idObj != null && int.TryParse(idObj.ToString(), out int oId))
                    return oId;
            }

            try
            {
                string decrypted = EncryptionHelper.Decrypt(orderEdidOrNumber);
                if (int.TryParse(decrypted, out int idVal))
                    return idVal;
            }
            catch { }

            if (int.TryParse(orderEdidOrNumber, out int rawVal))
                return rawVal;

            SqlParameter[] sp = { new SqlParameter("@OrderNumber", orderEdidOrNumber.Trim()) };
            var scalarObj = _client.ExecuteScalar<object>("SELECT id FROM OrderMaster WHERE orderNumber = @OrderNumber", sp, CommandType.Text);
            if (scalarObj != null && int.TryParse(scalarObj.ToString(), out int foundId))
                return foundId;

            throw new HoneyErpException(ErrorCode.NotFound, "Order not found");
        }

        public GetOrderCommentsRes GetComments(string orderEdid)
        {
            int orderId = ResolveOrderId(orderEdid);
            SqlParameter[] p = { new SqlParameter("@OrderId", orderId) };

            string    query = "SELECT id,text,createdAt,author FROM OrderComment WHERE orderId=@OrderId ORDER BY createdAt ASC";
            DataTable dt    = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            GetOrderCommentsRes res = new GetOrderCommentsRes();
            foreach (DataRow row in dt.Rows)
            {
                res.Items.Add(new OrderCommentItem
                {
                    Edid      = EncryptionHelper.Encrypt(row.Field<int>("id").ToString()),
                    Text      = row.Field<string?>("text"),
                    Author    = row.Field<string?>("author"),
                    CreatedAt = row.Field<DateTime>("createdAt")
                });
            }
            return res;
        }

        public string AddComment(string orderEdid, AddCommentReq req)
        {
            int orderId = ResolveOrderId(orderEdid);

            SqlParameter[] p = new SqlParameter[4];
            p[0] = new SqlParameter("@OrderId",   orderId);
            p[1] = new SqlParameter("@Text",      Utilities.DbValue(req.Text));
            p[2] = new SqlParameter("@Author",    Utilities.DbValue(req.Author));
            p[3] = new SqlParameter("@CreatedAt", DateTime.UtcNow);

            string query = "INSERT INTO OrderComment (orderId,text,author,createdAt) OUTPUT INSERTED.Id VALUES (@OrderId,@Text,@Author,@CreatedAt)";
            int newId = _client.ExecuteScalar<int>(query, p, CommandType.Text);
            return EncryptionHelper.Encrypt(newId.ToString());
        }

        // ── Package ───────────────────────────────────────────────────────────
        public GetPackageRes? GetPackage(string orderEdid)
        {
            string orderId = EncryptionHelper.Decrypt(orderEdid);
            SqlParameter[] p = { new SqlParameter("@OrderId", orderId) };

            string query = @"
                SELECT op.id, op.length, op.width, op.height, op.weight, op.status,
                       cm.name AS courier, ch.courierId, ch.trackingNumber AS tracking, ch.receiptDocId, ch.totalWeight AS handoverWeight,
                       ch.handedOverBy, ch.handedOverAt
                FROM OrderPackage op
                LEFT JOIN CourierHandover ch ON op.orderId = ch.orderMasterId
                LEFT JOIN CourierMaster cm ON ch.courierId = cm.id
                WHERE op.orderId = @OrderId";
            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            string? slipDoc = null;
            if (dt.Columns.Contains("receiptDocId") && !row.IsNull("receiptDocId"))
            {
                slipDoc = EncryptionHelper.Encrypt(Convert.ToString(row["receiptDocId"]));
            }

            return new GetPackageRes
            {
                Edid              = EncryptionHelper.Encrypt(row.Field<int>("id").ToString()),
                Length            = row.Field<decimal?>("length"),
                Width             = row.Field<decimal?>("width"),
                Height            = row.Field<decimal?>("height"),
                Weight            = row.Field<decimal?>("weight") ?? (row.IsNull("handoverWeight") ? (decimal?)null : Convert.ToDecimal(row["handoverWeight"])),
                Courier           = row.Field<string?>("courier"),
                CourierId         = row.IsNull("courierId") ? (int?)null : Convert.ToInt32(row["courierId"]),
                Status            = Enum.TryParse<PackageStatus>(row.Field<string?>("status"), true, out var pkgSt) ? pkgSt : null,
                Tracking          = row.Field<string?>("tracking"),
                SlipDocument      = slipDoc,
                HandedOverBy      = row.Field<string?>("handedOverBy"),
                HandedOverAt      = row.IsNull("handedOverAt") ? (DateTime?)null : Convert.ToDateTime(row["handedOverAt"])
            };
        }

        public SavePackageRes SavePackage(string orderEdid, SavePackageReq req)
        {
            string orderId = EncryptionHelper.Decrypt(orderEdid);

            SqlParameter[] p = new SqlParameter[6];
            p[0] = new SqlParameter("@OrderId", orderId);
            p[1] = new SqlParameter("@Length",  Utilities.DbValue(req.Length));
            p[2] = new SqlParameter("@Width",   Utilities.DbValue(req.Width));
            p[3] = new SqlParameter("@Height",  Utilities.DbValue(req.Height));
            p[4] = new SqlParameter("@Weight",  Utilities.DbValue(req.Weight));
            p[5] = new SqlParameter("@Status",  Utilities.DbValue(req.Status?.ToString()));

            string query = @"
                IF EXISTS (SELECT 1 FROM OrderPackage WHERE orderId = @OrderId)
                BEGIN
                    UPDATE OrderPackage
                    SET length = COALESCE(@Length, length),
                        width = COALESCE(@Width, width),
                        height = COALESCE(@Height, height),
                        weight = COALESCE(@Weight, weight),
                        status = COALESCE(@Status, status)
                    WHERE orderId = @OrderId;

                    SELECT id FROM OrderPackage WHERE orderId = @OrderId;
                END
                ELSE
                BEGIN
                    INSERT INTO OrderPackage (orderId, length, width, height, weight, status)
                    OUTPUT INSERTED.Id
                    VALUES (@OrderId, @Length, @Width, @Height, @Weight, @Status);
                END";

            int packageId = _client.ExecuteScalar<int>(query, p, CommandType.Text);
            return new SavePackageRes { Edid = EncryptionHelper.Encrypt(packageId.ToString()) };
        }

        public GetOrderFullDetailsRes? GetOrderFullDetails(string orderNumber, string? orderEdid = null)
        {
            string cleanOrderNumber = (orderNumber ?? string.Empty).Trim();
            int? targetOrderId = null;

            if (!string.IsNullOrWhiteSpace(orderEdid))
            {
                targetOrderId = EncryptionHelper.Decrypt<int?>(orderEdid);
            }

            if (targetOrderId == null && !string.IsNullOrWhiteSpace(cleanOrderNumber))
            {
                SqlParameter[] np = { new SqlParameter("@OrderNumber", cleanOrderNumber) };
                var idObj = _client.ExecuteScalar<object>("SELECT id FROM OrderMaster WHERE orderNumber = @OrderNumber", np, CommandType.Text);
                if (idObj != null && int.TryParse(idObj.ToString(), out int foundId))
                {
                    targetOrderId = foundId;
                }
            }

            if (targetOrderId == null)
                return null;

            SqlParameter[] p = new SqlParameter[]
            {
                new SqlParameter("@OrderId", targetOrderId.Value)
            };

            string query = @"
        SELECT 
            o.id AS OrderId,
            o.orderNumber,
            o.deliveryType,
            o.subtotal,
            o.deliveryChargeGiven AS deliveryCharge,
            o.discount,
            o.totalAmount,
            o.paymentStatus,
            o.status AS orderStatus,
            o.createdAt AS orderCreatedAt,

            c.id AS CustomerId,
            c.name AS CustomerName,
            c.mobile AS CustomerMobile,
            c.email AS CustomerEmail,
            c.type AS CustomerType,
            c.totalOrders AS CustomerTotalOrders,
            c.totalSpent AS CustomerTotalSpent,

            a.id AS AddressId,
            a.addressType,
            a.recipientName,
            a.houseStreet,
            a.areaLocality,
            a.landmark,
            a.city,
            a.district,
            a.state,
            a.pincode,
            a.deliveryInstructions,
            a.primaryMobile,
            a.secondaryMobile,

            oi.id AS orderItemId,
            oi.productId,
            oi.productNameSnapshot,
            oi.unitPrice,
            oi.quantity,
            oi.isCombo,
            oi.productPricingId,
            p.name AS productName,
            p.sku AS productSku,
            ISNULL(pp.mrp, (SELECT TOP 1 mrp FROM ProductPricing WHERE productId = p.id AND isActive = 1 ORDER BY id DESC)) AS productMrp,
            CASE 
                WHEN p.isCombo = 1 THEN ISNULL((
                    SELECT MIN(FLOOR(ISNULL(c.stock, 0) / NULLIF(pci2.quantity, 0))) 
                    FROM ProductComboItem pci2
                    OUTER APPLY (SELECT SUM(pm.remainingQty) AS stock FROM ProductBatchMaster pm WHERE pm.productId = pci2.productId AND pm.isActive = 1) c
                    WHERE pci2.comboProductId = p.id), 0)
                ELSE ISNULL((SELECT SUM(pb.remainingQty) FROM ProductBatchMaster pb WHERE pb.productId = p.id AND pb.isActive = 1), 0) 
            END AS productStock,
            CAST(NULL AS VARCHAR(500)) AS productImage,
            p.isActive AS productActive,
            pp.mrp AS pricingMrp,

            pci.productId AS comboChildProductId,
            pci.quantity AS comboChildQuantity,
            cp.name AS comboChildProductName,
            cp.sku AS comboChildSku,
            ISNULL((SELECT SUM(cpb.remainingQty) FROM ProductBatchMaster cpb WHERE cpb.productId = cp.id AND cpb.isActive = 1), 0) AS comboChildStock,
            cp.isActive AS comboChildActive,
            CAST(NULL AS NVARCHAR(500)) AS comboChildImage,

            op.id AS packageId,
            op.length AS packageLength,
            op.width AS packageWidth,
            op.height AS packageHeight,
            op.weight AS packageWeight,
            op.status AS packageStatus,

            cm.name AS courier,
            ch.courierId,
            ch.trackingNumber AS tracking,
            ch.receiptDocId,
            ch.totalWeight AS handoverWeight,
            ch.handedOverBy,
            ch.handedOverAt,

            oc.id AS commentId,
            oc.text AS commentText,
            oc.createdAt AS commentCreatedAt,
            oc.author AS commentAuthor

        FROM OrderMaster o
        LEFT JOIN CustomerMaster c ON o.customerId = c.id
        LEFT JOIN AddressMaster a ON o.deliveryAddressId = a.id
        LEFT JOIN OrderItem oi ON o.id = oi.orderId
        LEFT JOIN ProductMaster p ON oi.productId = p.id
        LEFT JOIN ProductPricing pp ON oi.productPricingId = pp.id
        LEFT JOIN ProductComboItem pci ON oi.isCombo = 1 AND oi.productId = pci.comboProductId
        LEFT JOIN ProductMaster cp ON pci.productId = cp.id
        LEFT JOIN OrderPackage op ON o.id = op.orderId
        LEFT JOIN CourierHandover ch ON o.id = ch.orderMasterId
        LEFT JOIN CourierMaster cm ON ch.courierId = cm.id
        LEFT JOIN OrderComment oc ON o.id = oc.orderId
        WHERE o.id = @OrderId
        ORDER BY oi.id, oc.createdAt";

            DataSet ds = _client.ExecuteAdapter(query, p, CommandType.Text);
            DataTable dt = ds.Tables[0];

            if (dt.Rows.Count == 0)
                return null;

            DataRow row = dt.Rows[0];
            int resolvedOrderId = Convert.ToInt32(row["OrderId"]);

            GetOrderFullDetailsRes res = new GetOrderFullDetailsRes
            {
                Edid = EncryptionHelper.Encrypt(resolvedOrderId.ToString()),
                OrderNumber = row.Field<string>("orderNumber") ?? string.Empty,
                DeliveryType = row.Field<string?>("deliveryType"),
                Subtotal = row.IsNull("subtotal") ? 0m : Convert.ToDecimal(row["subtotal"]),
                DeliveryCharge = row.IsNull("deliveryCharge") ? 0m : Convert.ToDecimal(row["deliveryCharge"]),
                Discount = row.IsNull("discount") ? 0m : Convert.ToDecimal(row["discount"]),
                TotalAmount = row.IsNull("totalAmount") ? 0m : Convert.ToDecimal(row["totalAmount"]),
                PaymentStatus = row.Field<string?>("paymentStatus") ?? string.Empty,
                Status = row.Field<string?>("orderStatus") ?? string.Empty,
                CreatedAt = row.Field<DateTime>("orderCreatedAt")
            };

            // Customer
            if (!row.IsNull("CustomerId"))
            {
                int customerId = Convert.ToInt32(row["CustomerId"]);

                res.Customer = new OrderCustomerDetail
                {
                    CustomerEdid = EncryptionHelper.Encrypt(customerId.ToString()),
                    Name = row.Field<string?>("CustomerName") ?? string.Empty,
                    Mobile = row.Field<string?>("CustomerMobile") ?? string.Empty,
                    Email = row.Field<string?>("CustomerEmail"),
                    Type = row.Field<string?>("CustomerType"),
                    TotalOrders = row.IsNull("CustomerTotalOrders") ? (int?)null : Convert.ToInt32(row["CustomerTotalOrders"]),
                    TotalSpent = row.IsNull("CustomerTotalSpent") ? (decimal?)null : Convert.ToDecimal(row["CustomerTotalSpent"])
                };
            }

            // Delivery Address
            if (!row.IsNull("AddressId"))
            {
                int addressId = Convert.ToInt32(row["AddressId"]);

                res.DeliveryAddress = new OrderAddressDetail
                {
                    AddressEdid = EncryptionHelper.Encrypt(addressId.ToString()),
                    AddressType = row.Field<string?>("addressType"),
                    RecipientName = row.Field<string?>("recipientName"),
                    HouseStreet = row.Field<string?>("houseStreet"),
                    AreaLocality = row.Field<string?>("areaLocality"),
                    Landmark = row.Field<string?>("landmark"),
                    City = row.Field<string?>("city"),
                    District = row.Field<string?>("district"),
                    State = row.Field<string?>("state"),
                    Pincode = row.Field<string?>("pincode"),
                    DeliveryInstructions = row.Field<string?>("deliveryInstructions"),
                    PrimaryMobile = row.Field<string?>("primaryMobile"),
                    SecondaryMobile = row.Field<string?>("secondaryMobile")
                };
            }

            // Package Details
            if (!row.IsNull("packageId") || !row.IsNull("courierId") || !row.IsNull("tracking"))
            {
                string? slipDoc = null;
                if (dt.Columns.Contains("receiptDocId") && !row.IsNull("receiptDocId"))
                    slipDoc = EncryptionHelper.Encrypt(Convert.ToString(row["receiptDocId"]));

                res.Package = new OrderPackageDetail
                {
                    Edid = row.IsNull("packageId") ? null : EncryptionHelper.Encrypt(Convert.ToString(row["packageId"])),
                    Length = row.IsNull("packageLength") ? (decimal?)null : Convert.ToDecimal(row["packageLength"]),
                    Width = row.IsNull("packageWidth") ? (decimal?)null : Convert.ToDecimal(row["packageWidth"]),
                    Height = row.IsNull("packageHeight") ? (decimal?)null : Convert.ToDecimal(row["packageHeight"]),
                    Weight = !row.IsNull("packageWeight") ? Convert.ToDecimal(row["packageWeight"]) : (!row.IsNull("handoverWeight") ? Convert.ToDecimal(row["handoverWeight"]) : (decimal?)null),
                    Courier = row.Field<string?>("courier"),
                    CourierId = row.IsNull("courierId") ? (int?)null : Convert.ToInt32(row["courierId"]),
                    Status = Enum.TryParse<PackageStatus>(row.Field<string?>("packageStatus"), true, out var pkgSt) ? pkgSt : null,
                    Tracking = row.Field<string?>("tracking"),
                    SlipDocument = slipDoc,
                    HandedOverBy = row.Field<string?>("handedOverBy"),
                    HandedOverAt = row.IsNull("handedOverAt") ? (DateTime?)null : Convert.ToDateTime(row["handedOverAt"])
                };
            }

            // Order Items, Combo Items & Comments
            Dictionary<int, OrderDetailItem> itemLookup = new Dictionary<int, OrderDetailItem>();
            List<int> addedCommentIds = new List<int>();
            List<string> addedComboItemKeys = new List<string>();

            foreach (DataRow itemRow in dt.Rows)
            {
                if (!itemRow.IsNull("orderItemId"))
                {
                    int orderItemId = Convert.ToInt32(itemRow["orderItemId"]);

                    if (!itemLookup.ContainsKey(orderItemId))
                    {
                        int productId = Convert.ToInt32(itemRow["productId"]);
                        bool isCombo = !itemRow.IsNull("isCombo") && Convert.ToBoolean(itemRow["isCombo"]);
                        decimal unitPrice = itemRow.IsNull("unitPrice") ? 0m : Convert.ToDecimal(itemRow["unitPrice"]);
                        int quantity = itemRow.IsNull("quantity") ? 0 : Convert.ToInt32(itemRow["quantity"]);

                        decimal mrp = 0m;
                        if (!itemRow.IsNull("productMrp"))
                            mrp = Convert.ToDecimal(itemRow["productMrp"]);
                        else if (!itemRow.IsNull("pricingMrp"))
                            mrp = Convert.ToDecimal(itemRow["pricingMrp"]);

                        string? pricingEdid = null;
                        if (!itemRow.IsNull("productPricingId"))
                        {
                            pricingEdid = EncryptionHelper.Encrypt(Convert.ToString(itemRow["productPricingId"]));
                        }

                        OrderDetailItem item = new OrderDetailItem
                        {
                            OrderItemEdid = EncryptionHelper.Encrypt(orderItemId.ToString()),
                            ProductEdid = EncryptionHelper.Encrypt(productId.ToString()),
                            ProductName = itemRow.Field<string>("productName"),
                            ProductNameSnapshot = itemRow.Field<string?>("productNameSnapshot"),
                            Sku = itemRow.Field<string?>("productSku"),
                            Image = itemRow.Field<string?>("productImage"),
                            UnitPrice = unitPrice,
                            Quantity = quantity,
                            TotalPrice = unitPrice * quantity,
                            Mrp = mrp,
                            Stock = itemRow.IsNull("productStock") ? 0m : Convert.ToDecimal(itemRow["productStock"]),
                            IsActive = !itemRow.IsNull("productActive") && Convert.ToBoolean(itemRow["productActive"]),
                            IsCombo = isCombo,
                            ProductPricingEdid = pricingEdid
                        };

                        itemLookup.Add(orderItemId, item);
                        res.Items.Add(item);
                    }

                    // Combo Items
                    if (!itemRow.IsNull("comboChildProductId"))
                    {
                        int childProductId = Convert.ToInt32(itemRow["comboChildProductId"]);
                        string comboItemKey = orderItemId.ToString() + "_" + childProductId.ToString();

                        if (!addedComboItemKeys.Contains(comboItemKey))
                        {
                            OrderDetailItem item = itemLookup[orderItemId];

                            OrderComboItemDetail comboItem = new OrderComboItemDetail
                            {
                                ProductEdid = EncryptionHelper.Encrypt(childProductId.ToString()),
                                ProductName = itemRow.Field<string?>("comboChildProductName") ?? string.Empty,
                                Sku = itemRow.Field<string?>("comboChildSku"),
                                Quantity = itemRow.IsNull("comboChildQuantity") ? 0m : Convert.ToDecimal(itemRow["comboChildQuantity"]),
                                Stock = itemRow.IsNull("comboChildStock") ? 0m : Convert.ToDecimal(itemRow["comboChildStock"]),
                                Active = !itemRow.IsNull("comboChildActive") && Convert.ToBoolean(itemRow["comboChildActive"]),
                                Image = itemRow.Field<string?>("comboChildImage")
                            };

                            item.ComboItems.Add(comboItem);
                            addedComboItemKeys.Add(comboItemKey);
                        }
                    }
                }

                // Comments
                if (!itemRow.IsNull("commentId"))
                {
                    int commentId = Convert.ToInt32(itemRow["commentId"]);

                    if (!addedCommentIds.Contains(commentId))
                    {
                        OrderCommentItem comment = new OrderCommentItem
                        {
                            Edid = EncryptionHelper.Encrypt(commentId.ToString()),
                            Text = itemRow.Field<string?>("commentText"),
                            Author = itemRow.Field<string?>("commentAuthor"),
                            CreatedAt = itemRow.Field<DateTime>("commentCreatedAt")
                        };

                        res.Comments.Add(comment);
                        addedCommentIds.Add(commentId);
                    }
                }
            }

            res.TotalItemsCount = res.Items.Sum(i => i.Quantity);

            // Retrieve Order Payments
            try
            {
                SqlParameter[] paymentParam = { new SqlParameter("@OrderId", resolvedOrderId) };
                string paymentQuery = @"SELECT id, orderId, transactionNumber, paymentMethod, paymentType, amount, status, paymentReference, paymentDate, remark, docId, createdAt
                                       FROM OrderPayment WHERE orderId = @OrderId ORDER BY createdAt DESC";
                DataTable paymentDt = _client.ExecuteAdapter(paymentQuery, paymentParam, CommandType.Text).Tables[0];
                decimal totalPaid = 0m;
                foreach (DataRow pRow in paymentDt.Rows)
                {
                    var pStatusEnum = Utilities.ParseEnum<OrderPaymentStatus>(pRow.Field<string>("status")) ?? OrderPaymentStatus.Success;
                    var pMethodEnum = Utilities.ParseEnum<OrderPaymentMethod>(pRow.Field<string>("paymentMethod")) ?? OrderPaymentMethod.UPI;
                    var pTypeEnum = Utilities.ParseEnum<OrderPaymentType>(pRow.Field<string>("paymentType")) ?? OrderPaymentType.FullPayment;

                    decimal pAmount = pRow.Field<decimal>("amount");
                    if (pStatusEnum == OrderPaymentStatus.Success)
                        totalPaid += pAmount;
                    else if (pStatusEnum == OrderPaymentStatus.Refunded)
                        totalPaid -= pAmount;

                    int? docId = pRow.IsNull("docId") ? (int?)null : Convert.ToInt32(pRow["docId"]);
                    string? docEdid = docId.HasValue ? EncryptionHelper.Encrypt(docId.Value.ToString()) : null;

                    res.Payments.Add(new OrderPaymentItem
                    {
                        Edid = EncryptionHelper.Encrypt(pRow.Field<int>("id").ToString()),
                        OrderEdid = res.Edid,
                        OrderNumber = res.OrderNumber,
                        TransactionNumber = pRow.Field<string?>("transactionNumber") ?? "",
                        PaymentMethod = pMethodEnum,
                        PaymentType = pTypeEnum,
                        Amount = pAmount,
                        Status = pStatusEnum,
                        PaymentReference = pRow.Field<string?>("paymentReference"),
                        PaymentDate = pRow.Field<DateTime?>("paymentDate"),
                        Remark = pRow.Field<string?>("remark"),
                        DocId = docId,
                        DocEdid = docEdid,
                        CreatedAt = pRow.Field<DateTime>("createdAt")
                    });
                }
                res.AmountPaid = totalPaid;
                res.BalanceAmount = Math.Max(0m, res.TotalAmount - totalPaid);
                res.PaymentStatus = (res.AmountPaid <= 0
                    ? PaymentStatus.Unpaid
                    : (res.AmountPaid < res.TotalAmount ? PaymentStatus.PartiallyPaid : PaymentStatus.Paid)).ToString();
            }
            catch
            {
                res.AmountPaid = 0m;
                res.BalanceAmount = res.TotalAmount;
            }

            // Retrieve Order Physical Bottle Items
            res.ProductItems = GetOrderProductItems(resolvedOrderId);

            return res;
        }

        // ── Physical Bottles & Packing Station ────────────────────────────────

        public List<OrderProductItemDto> GetOrderProductItems(int orderId)
        {
            SqlParameter[] p = { new SqlParameter("@OrderId", orderId) };
            string query = @"
                SELECT opi.id, opi.orderProductMstId, opi.productSingleId, opi.productBatchId, opi.status, opi.packedBy, opi.packedAt,
                       p.name AS productName, p.sku AS productSku,
                       pb.batchNumber, oi.productNameSnapshot AS orderItemProductName
                FROM OrderProductItem opi
                JOIN OrderProductMaster opm ON opi.orderProductMstId = opm.id
                JOIN ProductMaster p ON opi.productSingleId = p.id
                LEFT JOIN OrderItem oi ON opm.orderId = oi.orderId AND opm.productId = oi.productId
                LEFT JOIN ProductBatchMaster pb ON opi.productBatchId = pb.id
                WHERE opm.orderId = @OrderId
                ORDER BY opi.id ASC";

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];
            List<OrderProductItemDto> list = new List<OrderProductItemDto>();

            foreach (DataRow r in dt.Rows)
            {
                int opiId = r.Field<int>("id");
                int opmId = r.Field<int>("orderProductMstId");
                int singleId = r.Field<int>("productSingleId");
                int? batchId = r.IsNull("productBatchId") ? (int?)null : r.Field<int>("productBatchId");

                list.Add(new OrderProductItemDto
                {
                    Id = opiId,
                    Edid = EncryptionHelper.Encrypt(opiId.ToString()),
                    OrderProductMstId = opmId,
                    OrderProductMstEdid = EncryptionHelper.Encrypt(opmId.ToString()),
                    ProductSingleId = singleId,
                    ProductSingleEdid = EncryptionHelper.Encrypt(singleId.ToString()),
                    ProductBatchId = batchId,
                    ProductBatchEdid = batchId.HasValue ? EncryptionHelper.Encrypt(batchId.Value.ToString()) : null,
                    Status = Enum.TryParse<OrderProductItemStatus>(r.Field<string>("status"), true, out var parsedStatus)
                        ? parsedStatus
                        : OrderProductItemStatus.Pending,
                    PackedBy = r.Field<string?>("packedBy"),
                    PackedAt = r.Field<DateTime?>("packedAt"),
                    ProductName = r.Field<string>("productName") ?? "",
                    ProductSku = r.Field<string?>("productSku"),
                    BatchNumber = r.Field<string?>("batchNumber"),
                    OrderItemProductName = r.Field<string?>("orderItemProductName")
                });
            }

            return list;
        }

        public List<ProductAvailableBatchDto> GetAvailableBatches(IEnumerable<int> productIds)
        {
            var ids = productIds.Distinct().ToList();
            if (ids.Count == 0) return new List<ProductAvailableBatchDto>();

            string inClause = string.Join(",", ids);
            string query = $@"
                SELECT id, productId, batchNumber, remainingQty, isActive
                FROM ProductBatchMaster
                WHERE productId IN ({inClause}) AND isActive = 1 AND remainingQty > 0
                ORDER BY id ASC";

            DataTable dt = _client.ExecuteAdapter(query, Array.Empty<SqlParameter>(), CommandType.Text).Tables[0];
            List<ProductAvailableBatchDto> list = new List<ProductAvailableBatchDto>();

            foreach (DataRow r in dt.Rows)
            {
                int bId = r.Field<int>("id");
                int pId = r.Field<int>("productId");
                list.Add(new ProductAvailableBatchDto
                {
                    Id = bId,
                    Edid = EncryptionHelper.Encrypt(bId.ToString()),
                    ProductId = pId,
                    ProductEdid = EncryptionHelper.Encrypt(pId.ToString()),
                    BatchNumber = r.Field<string>("batchNumber") ?? "",
                    RemainingQty = r.Field<decimal>("remainingQty"),
                    IsActive = r.Field<bool>("isActive")
                });
            }

            return list;
        }

        public GetOrderPackingDetailsRes? GetOrderPackingDetails(string orderNumber)
        {
            var full = GetOrderFullDetails(orderNumber);
            if (full == null) return null;

            int orderId = int.Parse(EncryptionHelper.Decrypt(full.Edid));
            var items = GetOrderProductItems(orderId);
            var productIds = items.Select(i => i.ProductSingleId).Distinct().ToList();
            var batches = GetAvailableBatches(productIds);

            string usageSql = @"
                SELECT mbu.quantityUsed, mbu.createdAt, rm.id AS materialId, rm.name AS materialName, mb.batchNumber
                FROM MaterialBatchUsage mbu
                INNER JOIN MaterialBatchMaster mb ON mbu.materialBatchId = mb.id
                INNER JOIN RawMaterialMaster rm ON mbu.materialId = rm.id
                WHERE mbu.remark LIKE 'Order #' + @OrderNumber + ' %'
                ORDER BY mbu.id ASC";
            SqlParameter[] uP = { new SqlParameter("@OrderNumber", full.OrderNumber.Trim().TrimStart('#')) };
            DataTable dtUsage = _client.ExecuteAdapter(usageSql, uP, CommandType.Text).Tables[0];
            var usedMaterials = new List<UsedPackingMaterialDto>();
            foreach (DataRow r in dtUsage.Rows)
            {
                usedMaterials.Add(new UsedPackingMaterialDto
                {
                    MaterialEdid = EncryptionHelper.Encrypt(r.Field<int>("materialId").ToString()),
                    MaterialName = r.Field<string>("materialName"),
                    BatchNumber = r.Field<string?>("batchNumber"),
                    QuantityUsed = r.Field<decimal>("quantityUsed"),
                    CreatedAt = r.Field<DateTime>("createdAt")
                });
            }

            return new GetOrderPackingDetailsRes
            {
                OrderEdid = full.Edid,
                OrderNumber = full.OrderNumber,
                Status = full.Status,
                CustomerName = full.Customer?.Name,
                CustomerMobile = full.Customer?.Mobile,
                DeliveryType = full.DeliveryType,
                DeliveryAddress = full.DeliveryAddress,
                Package = full.Package,
                Items = items,
                AvailableBatches = batches,
                UsedMaterials = usedMaterials
            };
        }

        public GetOrderBottlesRes GetOrderBottles(string orderNumber)
        {
            if (string.IsNullOrWhiteSpace(orderNumber))
                throw new HoneyErpException(ErrorCode.InvalidData, "Order number is required.");

            SqlParameter[] np = { new SqlParameter("@OrderNumber", orderNumber.Trim()) };
            var idObj = _client.ExecuteScalar<object>("SELECT id FROM OrderMaster WHERE orderNumber = @OrderNumber", np, CommandType.Text);

            if (idObj == null || !int.TryParse(idObj.ToString(), out int orderId) || orderId == 0)
                throw new HoneyErpException(ErrorCode.NotFound, $"Order '{orderNumber}' not found.");

            var items = GetOrderProductItems(orderId);

            return new GetOrderBottlesRes
            {
                OrderEdid = EncryptionHelper.Encrypt(orderId.ToString()),
                OrderNumber = orderNumber.Trim(),
                Items = items
            };
        }

        public bool ConfirmPacking(ConfirmPackingReq req)
        {
            int orderId = int.Parse(EncryptionHelper.Decrypt(req.OrderEdid));

            SqlParameter[] orderP = { new SqlParameter("@OrderId", orderId) };
            string orderNumber = _client.ExecuteScalar<string>("SELECT orderNumber FROM OrderMaster WHERE id = @OrderId", orderP, CommandType.Text) ?? $"ORD-{orderId}";

            // 1. Assign product batches to OrderProductItems and track quantities per batch
            var assignedBottles = new List<(int opiId, int productId, string productName, int batchId, decimal quantity)>();

            foreach (var b in req.BottleAssignments)
            {
                int opiId = int.Parse(EncryptionHelper.Decrypt(b.OrderProductItemEdid));
                int batchId = int.Parse(EncryptionHelper.Decrypt(b.ProductBatchEdid));
                decimal qty = (b.Quantity.HasValue && b.Quantity.Value > 0) ? b.Quantity.Value : 1.00m;

                // Get product details for this OrderProductItem
                SqlParameter[] prodP = { new SqlParameter("@OpiId", opiId) };
                string getProdSql = @"SELECT opi.productSingleId, p.name 
                                      FROM OrderProductItem opi 
                                      INNER JOIN ProductMaster p ON opi.productSingleId = p.id 
                                      WHERE opi.id = @OpiId";
                DataTable prodDt = _client.ExecuteAdapter(getProdSql, prodP, CommandType.Text).Tables[0];
                int productId = prodDt.Rows.Count > 0 ? prodDt.Rows[0].Field<int>("productSingleId") : 0;
                string productName = prodDt.Rows.Count > 0 ? (prodDt.Rows[0].Field<string>("name") ?? "Product") : "Product";

                SqlParameter[] p = new SqlParameter[5];
                p[0] = new SqlParameter("@Id", opiId);
                p[1] = new SqlParameter("@ProductBatchId", batchId);
                p[2] = new SqlParameter("@PackedBy", "testing");
                p[3] = new SqlParameter("@PackedAt", DateTime.UtcNow);
                p[4] = new SqlParameter("@Status", OrderProductItemStatus.Packed.ToString());

                string updateOpi = @"UPDATE OrderProductItem 
                                     SET productBatchId = @ProductBatchId, status = @Status, packedBy = @PackedBy, packedAt = @PackedAt 
                                     WHERE id = @Id";
                _client.ExecuteNonQuery(updateOpi, p, CommandType.Text);

                assignedBottles.Add((opiId, productId, productName, batchId, qty));
            }

            // Group assignments by (productId, batchId) to deduct stock and record total usage per batch dynamically
            var batchGroups = assignedBottles
                .GroupBy(x => new { x.productId, x.productName, x.batchId })
                .Select(g => new {
                    g.Key.productId,
                    g.Key.productName,
                    g.Key.batchId,
                    TotalQuantity = g.Sum(x => x.quantity)
                });

            foreach (var bg in batchGroups)
            {
                decimal quantityUsed = bg.TotalQuantity;

                SqlParameter[] spP = new SqlParameter[4];
                spP[0] = new SqlParameter("@Remark", $"Order #{orderNumber.Trim().TrimStart('#')} consumed {quantityUsed:0.##} of '{bg.productName}'");
                spP[1] = new SqlParameter("@ProductBatchId", bg.batchId);
                spP[2] = new SqlParameter("@QuantityUsed", quantityUsed);
                spP[3] = new SqlParameter("@ProductId", bg.productId);

                bool isSuccess = _client.ExecuteScalar<bool>("AddProductBatchUsage", spP, CommandType.StoredProcedure);
                if (!isSuccess)
                {
                    SqlParameter[] deductP = {
                        new SqlParameter("@BatchId", bg.batchId),
                        new SqlParameter("@Qty", quantityUsed)
                    };
                    string deductSql = @"UPDATE ProductBatchMaster 
                                         SET remainingQty = CASE WHEN remainingQty >= @Qty THEN remainingQty - @Qty ELSE 0 END 
                                         WHERE id = @BatchId";
                    _client.ExecuteNonQuery(deductSql, deductP, CommandType.Text);
                }
            }

            // 2. Process Used Packing Materials via FIFO batch deduction and record in MaterialBatchUsage
            if (req.PackingMaterials != null && req.PackingMaterials.Count > 0)
            {
                foreach (var pm in req.PackingMaterials)
                {
                    if (string.IsNullOrWhiteSpace(pm.MaterialEdid) || pm.Quantity <= 0)
                        continue;

                    int materialId = int.Parse(EncryptionHelper.Decrypt(pm.MaterialEdid));

                    // Get material name
                    SqlParameter[] matP = { new SqlParameter("@MatId", materialId) };
                    string materialName = _client.ExecuteScalar<string>("SELECT name FROM RawMaterialMaster WHERE id = @MatId", matP, CommandType.Text) ?? "Packing Material";

                    // Query available batches in FIFO order (ORDER BY id ASC)
                    string batchSql = @"
                        SELECT id, batchNumber, qtyRemaining 
                        FROM MaterialBatchMaster 
                        WHERE materialId = @MatId 
                          AND (isActive = 1 OR isActive IS NULL) 
                          AND qtyRemaining > 0 
                        ORDER BY id ASC";
                    SqlParameter[] bP = { new SqlParameter("@MatId", materialId) };
                    DataTable dtBatches = _client.ExecuteAdapter(batchSql, bP, CommandType.Text).Tables[0];

                    decimal totalAvailable = 0;
                    foreach (DataRow r in dtBatches.Rows)
                    {
                        totalAvailable += r.Field<decimal>("qtyRemaining");
                    }

                    if (totalAvailable < pm.Quantity)
                    {
                        throw new HoneyErpException(ErrorCode.InvalidData, $"Insufficient stock for '{materialName}'. Available: {totalAvailable:0.##}, Required: {pm.Quantity:0.##}.");
                    }

                    decimal remainingQty = pm.Quantity;
                    foreach (DataRow r in dtBatches.Rows)
                    {
                        if (remainingQty <= 0) break;
                        int batchId = r.Field<int>("id");
                        decimal batchRemaining = r.Field<decimal>("qtyRemaining");
                        decimal usedQty = Math.Min(remainingQty, batchRemaining);
                        if (usedQty <= 0) continue;

                        SqlParameter[] spP = new SqlParameter[4];
                        spP[0] = new SqlParameter("@Remark", $"Order #{orderNumber.Trim().TrimStart('#')} consumed {usedQty:0.##} of '{materialName}'");
                        spP[1] = new SqlParameter("@MaterialBatchId", batchId);
                        spP[2] = new SqlParameter("@QuantityUsed", usedQty);
                        spP[3] = new SqlParameter("@MaterialId", materialId);

                        bool isSuccess = _client.ExecuteScalar<bool>("AddMaterialBatchUsage", spP, CommandType.StoredProcedure);
                        if (!isSuccess)
                        {
                            throw new HoneyErpException(ErrorCode.InternalError, $"Failed to record batch usage for material '{materialName}'.");
                        }

                        remainingQty -= usedQty;
                    }
                }
            }

            // 3. Save Package (weight and dimensions are required)
            SavePackage(req.OrderEdid, new SavePackageReq
            {
                Length = req.Length,
                Width = req.Width,
                Height = req.Height,
                Weight = req.Weight,
                Status = PackageStatus.Packed
            });

            SqlParameter[] statusP = new SqlParameter[2];
            statusP[0] = new SqlParameter("@Id", orderId);
            statusP[1] = new SqlParameter("@Status", OrderStatus.ReadyToShip.ToString());
            bool packed = _client.ExecuteNonQuery("UPDATE OrderMaster SET status=@Status WHERE id=@Id", statusP, CommandType.Text) > 0;
            if (packed)
            {
                LogStatusTracking(orderId, OrderStatus.ReadyToShip.ToString(), $"Order packed ({req.BottleAssignments?.Count ?? 0} bottles, weight: {req.Weight ?? 0}kg)", !string.IsNullOrWhiteSpace(req.PackedBy) ? req.PackedBy : "EMP001", DateTime.UtcNow);
            }
            return packed;
        }

        public bool DispatchOrder(DispatchOrderReq req)
        {
            int orderId = int.Parse(EncryptionHelper.Decrypt(req.OrderEdid));

            // 1. Resolve CourierId from CourierMaster
            int courierId = 0;
            if (req.CourierId.HasValue && req.CourierId.Value > 0)
            {
                courierId = req.CourierId.Value;
            }
            else if (!string.IsNullOrWhiteSpace(req.CourierEdid))
            {
                int.TryParse(EncryptionHelper.Decrypt(req.CourierEdid), out courierId);
            }

            if (courierId == 0 && !string.IsNullOrWhiteSpace(req.Courier))
            {
                SqlParameter[] cp = { new SqlParameter("@Name", req.Courier.Trim()) };
                var cidObj = _client.ExecuteScalar<object>("SELECT id FROM CourierMaster WHERE name = @Name", cp, CommandType.Text);
                if (cidObj != null && int.TryParse(cidObj.ToString(), out int foundCid) && foundCid > 0)
                {
                    courierId = foundCid;
                }
                else
                {
                    SqlParameter[] insp = { new SqlParameter("@Name", req.Courier.Trim()) };
                    courierId = _client.ExecuteScalar<int>(
                        "INSERT INTO CourierMaster (name, isActive, createdAt) VALUES (@Name, 1, GETUTCDATE()); SELECT SCOPE_IDENTITY();",
                        insp, CommandType.Text);
                }
            }

            // Fallback to 1 (default active courier) if none matched
            if (courierId == 0)
            {
                var firstCid = _client.ExecuteScalar<object>("SELECT TOP 1 id FROM CourierMaster WHERE isActive = 1 ORDER BY id ASC", null, CommandType.Text);
                courierId = firstCid != null && int.TryParse(firstCid.ToString(), out int fc) ? fc : 1;
            }

            // 2. Resolve ReceiptDocId from SlipDocument
            int? receiptDocId = req.ReceiptDocId;
            if (!receiptDocId.HasValue && !string.IsNullOrWhiteSpace(req.SlipDocument))
            {
                try
                {
                    string decrypted = EncryptionHelper.Decrypt(req.SlipDocument);
                    if (int.TryParse(decrypted, out int docId))
                    {
                        receiptDocId = docId;
                    }
                }
                catch
                {
                    receiptDocId = null;
                }
            }

            // 3. Resolve TrackingNumber
            string trackingNumber = (req.TrackingNumber ?? req.Tracking ?? string.Empty).Trim();

            // 4. Resolve TotalWeight
            decimal? totalWeight = req.TotalWeight;
            if (!totalWeight.HasValue)
            {
                SqlParameter[] wp = { new SqlParameter("@OrderId", orderId) };
                var wObj = _client.ExecuteScalar<object>("SELECT weight FROM OrderPackage WHERE orderId = @OrderId", wp, CommandType.Text);
                if (wObj != null && decimal.TryParse(wObj.ToString(), out decimal pkgWeight))
                {
                    totalWeight = pkgWeight;
                }
            }

            // 5. HandedOverBy and HandedOverAt
            string handedOverBy = string.IsNullOrWhiteSpace(req.HandedOverBy) ? "Staff" : req.HandedOverBy.Trim();
            DateTime handedOverAt = req.HandedOverAt ?? DateTime.UtcNow;

            // 6. Upsert into CourierHandover
            SqlParameter[] chP = new SqlParameter[7];
            chP[0] = new SqlParameter("@OrderId",        orderId);
            chP[1] = new SqlParameter("@CourierId",      courierId);
            chP[2] = new SqlParameter("@ReceiptDocId",   Utilities.DbValue(receiptDocId));
            chP[3] = new SqlParameter("@TrackingNumber", Utilities.DbValue(trackingNumber));
            chP[4] = new SqlParameter("@TotalWeight",    Utilities.DbValue(totalWeight));
            chP[5] = new SqlParameter("@HandedOverBy",   Utilities.DbValue(handedOverBy));
            chP[6] = new SqlParameter("@HandedOverAt",   Utilities.DbValue(handedOverAt));

            string upsertHandover = @"
                IF EXISTS (SELECT 1 FROM CourierHandover WHERE orderMasterId = @OrderId)
                BEGIN
                    UPDATE CourierHandover 
                    SET courierId = @CourierId,
                        receiptDocId = @ReceiptDocId,
                        trackingNumber = @TrackingNumber,
                        totalWeight = @TotalWeight,
                        handedOverBy = @HandedOverBy,
                        handedOverAt = @HandedOverAt
                    WHERE orderMasterId = @OrderId;
                END
                ELSE
                BEGIN
                    INSERT INTO CourierHandover (orderMasterId, courierId, receiptDocId, trackingNumber, totalWeight, handedOverBy, handedOverAt)
                    VALUES (@OrderId, @CourierId, @ReceiptDocId, @TrackingNumber, @TotalWeight, @HandedOverBy, @HandedOverAt);
                END";
            _client.ExecuteNonQuery(upsertHandover, chP, CommandType.Text);

            // 7. Update status on OrderPackage (if it exists)
            SqlParameter[] pkgP = new SqlParameter[2];
            pkgP[0] = new SqlParameter("@OrderId", orderId);
            pkgP[1] = new SqlParameter("@Status", PackageStatus.Shipped.ToString());
            _client.ExecuteNonQuery("UPDATE OrderPackage SET status = @Status WHERE orderId = @OrderId", pkgP, CommandType.Text);

            // 8. Update status on OrderMaster
            SqlParameter[] statusP = new SqlParameter[2];
            statusP[0] = new SqlParameter("@Id", orderId);
            statusP[1] = new SqlParameter("@Status", OrderStatus.Shipped.ToString());
            bool dispatched = _client.ExecuteNonQuery("UPDATE OrderMaster SET status=@Status WHERE id=@Id", statusP, CommandType.Text) > 0;
            if (dispatched)
            {
                string courierName = !string.IsNullOrWhiteSpace(req.Courier) ? req.Courier : "Courier";
                string trackStr = !string.IsNullOrWhiteSpace(trackingNumber) ? $" Tracking: {trackingNumber}" : "";
                LogStatusTracking(orderId, OrderStatus.Shipped.ToString(), $"Dispatched via {courierName}.{trackStr}", handedOverBy, handedOverAt);

                // 9. Record courier/delivery charge into financial ledger (PaymentTransactionMaster)
                if (req.CourierCharge.HasValue && req.CourierCharge.Value > 0)
                {
                    try
                    {
                        string orderNum = _client.ExecuteScalar<string>(
                            "SELECT orderNumber FROM OrderMaster WHERE id = @Id",
                            new[] { new SqlParameter("@Id", orderId) },
                            CommandType.Text) ?? orderId.ToString();

                        string txnNum = Utilities.GeneratePublicId("TXN", 6);
                        string desc = $"Courier charge for Order #{orderNum} via {courierName}";
                        if (!string.IsNullOrWhiteSpace(trackingNumber))
                            desc += $" (AWB: {trackingNumber})";

                        SqlParameter[] tp = new SqlParameter[6];
                        tp[0] = new SqlParameter("@TransactionNumber", txnNum);
                        tp[1] = new SqlParameter("@Description",       desc);
                        tp[2] = new SqlParameter("@Amount",            req.CourierCharge.Value);
                        tp[3] = new SqlParameter("@Status",            PaymentType.Debit.ToString());
                        tp[4] = new SqlParameter("@Category",          TransactionCategory.Expense.ToString());
                        tp[5] = new SqlParameter("@IsActive",          true);

                        string ledgerQuery = @"
                            DECLARE @PrevBalance DECIMAL(18,2) = 0;
                            SELECT TOP 1 @PrevBalance = ISNULL(balance, 0) FROM PaymentTransactionMaster ORDER BY id DESC;
                            DECLARE @NewBalance DECIMAL(18,2) = @PrevBalance - @Amount;

                            INSERT INTO PaymentTransactionMaster (transactionNumber, remark, amount, status, category, isActive, balance, createdAt) 
                            VALUES (@TransactionNumber, @Description, @Amount, @Status, @Category, @IsActive, @NewBalance, SYSUTCDATETIME());";

                        _client.ExecuteNonQuery(ledgerQuery, tp, CommandType.Text);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error recording courier charge in financial ledger: {ex.Message}");
                    }
                }
            }
            return dispatched;
        }

        public PageResult<OrderListItem> GetReadyToShipOrders(GetOrderListReq req)
        {
            req.Status = OrderStatus.ReadyToShip.ToString();
            return GetAll(req);
        }

        public List<string> GetOrderNumbersByStatus(string status)
        {
            SqlParameter[] p = { new SqlParameter("@Status", status) };
            string query = @"SELECT o.orderNumber 
                             FROM OrderMaster o 
                             WHERE o.status = @Status 
                                OR REPLACE(o.status, ' ', '') = REPLACE(@Status, ' ', '')
                             ORDER BY o.id ASC";
            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];
            List<string> result = new List<string>();
            foreach (DataRow row in dt.Rows)
            {
                string? ord = row.Field<string>("orderNumber");
                if (!string.IsNullOrWhiteSpace(ord))
                    result.Add(ord);
            }
            return result;
        }
    }
}