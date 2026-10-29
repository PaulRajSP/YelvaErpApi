using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Repository
{
    public interface IPurchaseRepository
    {
        PageResult<PurchaseListItem> GetAll(GetPurchaseListReq req);
        GetPurchaseListRes  GetBySupplier(GetPurchasesBySupplierReq req);
        GetPurchaseByIdRes? GetById(string edid);
        List<PurchasePaymentItem> GetPayments(string purchaseEdid);
        CreatePurchaseRes   Create(CreatePurchaseReq req);
        int RecordPayment(string purchaseEdid, string supplierEdid, PayPurchaseDueReq req, string newStatus, decimal paidAmountDelta, string transactionNumber);
        bool UpdateStatus(string edid, string status);
        GetPurchaseItemsRes GetItems(GetPurchaseItemsReq req);
    }

    public class PurchaseRepository : IPurchaseRepository
    {
        private readonly DbClient _client;
        public PurchaseRepository(DbClient client) => _client = client;

        // ── Purchase ─────────────────────────────────────────────────────────

        public PageResult<PurchaseListItem> GetAll(GetPurchaseListReq req)
        {
            int pageNumber = req.Pagination.PageNumber > 0 ? req.Pagination.PageNumber : 1;
            int pageSize = req.Pagination.PageSize > 0 ? req.Pagination.PageSize : 10;
            int offset = (pageNumber - 1) * pageSize;

            string query = @"SELECT COUNT(*) OVER() AS TotalRecords, p.id, p.supplierId,
                            s.name AS supplierName, p.date, p.totalAmount, p.status,
                            p.paidAmount, (p.totalAmount - p.paidAmount) AS balance,
                            CAST(NULL AS NVARCHAR(255)) AS documentOriginalName
                     FROM PurchaseMaster p
                     INNER JOIN SupplierMaster s ON p.supplierId = s.id
                     WHERE (@Status IS NULL OR p.status = @Status)
                       AND (@FromDate IS NULL OR CAST(p.date AS DATE) >= CAST(@FromDate AS DATE))
                       AND (@ToDate IS NULL OR CAST(p.date AS DATE) <= CAST(@ToDate AS DATE))
                       AND (@SearchKeyword IS NULL OR s.name LIKE '%' + @SearchKeyword + '%' OR s.mobile = @SearchKeyword)
                     ORDER BY p.id DESC
                     OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            SqlParameter[] p = new SqlParameter[6];
            p[0] = new SqlParameter("@Offset", offset);
            p[1] = new SqlParameter("@PageSize", pageSize);
            p[2] = new SqlParameter("@Status", Utilities.DbValue(req.Status));
            p[3] = new SqlParameter("@FromDate", Utilities.DbValue(req.FromDate));
            p[4] = new SqlParameter("@ToDate", Utilities.DbValue(req.ToDate));
            p[5] = new SqlParameter("@SearchKeyword", Utilities.DbValue(req.SearchKeyword));

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            PageResult<PurchaseListItem> res = new PageResult<PurchaseListItem>
            {
                TotalCount = dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["TotalRecords"]) : 0,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            foreach (DataRow row in dt.Rows)
            {
                res.Items.Add(new PurchaseListItem
                {
                    Edid = EncryptionHelper.Encrypt(row.Field<int>("id").ToString()),
                    PublicId = row.Field<int>("id").ToString(),
                    SupplierEdid = EncryptionHelper.Encrypt(row.Field<int>("supplierId").ToString()),
                    SupplierName = row.Field<string>("supplierName") ?? string.Empty,
                    Date = row.Field<DateTime>("date"),
                    TotalAmount = row.Field<decimal>("totalAmount"),
                    PaidAmount = row.Field<decimal>("paidAmount"),
                    Balance = Convert.ToDecimal(row["balance"]),
                    Status = (Convert.ToDecimal(row["balance"]) <= 0 && row.Field<decimal>("totalAmount") > 0) ? "PAID" : row.Field<string>("status")!,
                });
            }

            return res;
        }

        public GetPurchaseListRes GetBySupplier(GetPurchasesBySupplierReq req)
        {
            int pageNumber = req.PageNumber > 0 ? req.PageNumber : 1;
            int pageSize = req.PageSize > 0 ? req.PageSize : 10;
            int offset = (pageNumber - 1) * pageSize;

            string query = @"SELECT COUNT(*) OVER() AS TotalRecords, p.id, p.supplierId, s.name AS supplierName,
                            p.date, p.totalAmount, p.status, p.paidAmount,
                            (p.totalAmount - p.paidAmount) AS balance,
                            CAST(NULL AS NVARCHAR(255)) AS documentOriginalName
                     FROM PurchaseMaster p
                     INNER JOIN SupplierMaster s ON p.supplierId = s.id
                     WHERE p.supplierId = @SupplierId
                       AND (@FromDate IS NULL OR CAST(p.date AS DATE) >= CAST(@FromDate AS DATE))
                       AND (@ToDate IS NULL OR CAST(p.date AS DATE) <= CAST(@ToDate AS DATE))
                       AND (@PaymentStatus IS NULL OR p.status = @PaymentStatus)
                     ORDER BY p.date DESC
                     OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            SqlParameter[] p = new SqlParameter[6];
            p[0] = new SqlParameter("@Offset", offset);
            p[1] = new SqlParameter("@PageSize", pageSize);
            p[2] = new SqlParameter("@SupplierId", EncryptionHelper.Decrypt(req.SupplierEdid));
            p[3] = new SqlParameter("@FromDate", Utilities.DbValue(req.FromDate));
            p[4] = new SqlParameter("@ToDate", Utilities.DbValue(req.ToDate));
            p[5] = new SqlParameter("@PaymentStatus", Utilities.DbValue(req.PaymentStatus));

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            GetPurchaseListRes res = new GetPurchaseListRes
            {
                TotalCount = dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["TotalRecords"]) : 0,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            foreach (DataRow row in dt.Rows)
            {
                res.Items.Add(new PurchaseListItem
                {
                    Edid = EncryptionHelper.Encrypt(row.Field<int>("id").ToString()),
                    PublicId = row.Field<int>("id").ToString(),
                    Date = row.Field<DateTime>("date"),
                    TotalAmount = row.Field<decimal>("totalAmount"),
                    PaidAmount = row.Field<decimal>("paidAmount"),
                    Balance = Convert.ToDecimal(row["balance"]),
                    Status = (Convert.ToDecimal(row["balance"]) <= 0 && row.Field<decimal>("totalAmount") > 0) ? "PAID" : row.Field<string>("status")!,
                });
            }

            return res;
        }

        public GetPurchaseByIdRes? GetById(string edid)
        {
            SqlParameter[] p = { new SqlParameter("@Id", EncryptionHelper.Decrypt(edid)) };

            string query = @"SELECT p.id, p.supplierId, s.name AS supplierName, p.date,
                            p.totalAmount, p.status, p.paidAmount,
                            (p.totalAmount - p.paidAmount) AS balance,
                            CAST(NULL AS NVARCHAR(255)) AS documentOriginalName
                     FROM PurchaseMaster p
                     INNER JOIN SupplierMaster s ON p.supplierId = s.id
                     WHERE p.id = @Id";

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            return new GetPurchaseByIdRes
            {
                Edid = EncryptionHelper.Encrypt(row.Field<int>("id").ToString()),
                PublicId = row.Field<int>("id").ToString(),
                SupplierEdid = EncryptionHelper.Encrypt(row.Field<int>("supplierId").ToString()),
                SupplierName = row.Field<string>("supplierName") ?? "",
                Date = row.Field<DateTime>("date"),
                TotalAmount = row.Field<decimal>("totalAmount"),
                PaidAmount = row.Field<decimal>("paidAmount"),
                Balance = Convert.ToDecimal(row["balance"]),
                Status = (Convert.ToDecimal(row["balance"]) <= 0 && row.Field<decimal>("totalAmount") > 0) ? "PAID" : row.Field<string>("status")!,
            };
        }

        public CreatePurchaseRes Create(CreatePurchaseReq req)
        {
            EnsurePurchasePaymentTable();
            string supplierId = EncryptionHelper.Decrypt(req.Purchase.SupplierEdid);

            req.Purchase.Balance = req.Purchase.TotalAmount - req.Purchase.PaidAmount;
            string? initialPaymentEdid = null;
            string? initialPaymentTransactionNumber = null;

            SqlParameter[] p = new SqlParameter[5];
            p[0] = new SqlParameter("@SupplierId", supplierId);
            p[1] = new SqlParameter("@TotalAmount", req.Purchase.TotalAmount);
            p[2] = new SqlParameter("@Status", req.Purchase.Status.ToString().ToUpper());
            p[3] = new SqlParameter("@Date", req.Purchase.Date);
            p[4] = new SqlParameter("@PaidAmount", req.Purchase.PaidAmount);
            string query = @"INSERT INTO PurchaseMaster (supplierId,date,totalAmount,status,paidAmount)
                     OUTPUT INSERTED.Id
                     VALUES (@SupplierId,@Date,@TotalAmount,@Status,@PaidAmount)";

            int purchaseId = _client.ExecuteScalar<int>(query, p, CommandType.Text);

            if (req.Purchase.PaidAmount > 0)
            {
                initialPaymentTransactionNumber = Utilities.GeneratePublicId("TXN", 6);
                int paymentId = InsertPaymentHistory(
                    purchaseId,
                    req.Purchase.PaidAmount,
                    initialPaymentTransactionNumber,
                    req.Purchase.InitialPaymentMethod ?? "Cash",
                    req.Purchase.InitialPaymentType,
                    OrderPaymentStatus.Success,
                    req.Purchase.InitialPaymentReference,
                    req.Purchase.InitialPaymentRemark,
                    req.Purchase.Date);
                initialPaymentEdid = EncryptionHelper.Encrypt(paymentId.ToString());
            }

            if (req.Purchase.Balance > 0)
            {
                SqlParameter[] bp = new SqlParameter[2];
                bp[0] = new SqlParameter("@SupplierId", supplierId);
                bp[1] = new SqlParameter("@Balance", req.Purchase.Balance);

                _client.ExecuteNonQuery(
                    "UPDATE SupplierMaster SET balance = ISNULL(balance, 0) + @Balance WHERE id = @SupplierId",
                    bp, CommandType.Text);
            }

            foreach (CreatePurchaseItemLine item in req.Items)
            {
                string materialId = EncryptionHelper.Decrypt(item.MaterialEdid);
                int? batchId = null;

                if (!string.IsNullOrWhiteSpace(item.BatchEdid))
                {
                    batchId = int.Parse(EncryptionHelper.Decrypt(item.BatchEdid));

                    SqlParameter[] bp = new SqlParameter[2];
                    bp[0] = new SqlParameter("@BatchId", batchId);
                    bp[1] = new SqlParameter("@Qty", item.Qty);

                    _client.ExecuteNonQuery(@"
                UPDATE MaterialBatchMaster
                SET qtyPurchased = ISNULL(qtyPurchased, 0) + @Qty,
                    qtyRemaining = ISNULL(qtyRemaining, 0) + @Qty
                WHERE id = @BatchId", bp, CommandType.Text);
                }
                else
                {
                    SqlParameter[] bp = new SqlParameter[2];
                    bp[0] = new SqlParameter("@MaterialId", materialId);
                    bp[1] = new SqlParameter("@Qty", item.Qty);

                    object? bObj = _client.ExecuteScalar<object>(@"
                        DECLARE @bId INT = (SELECT TOP 1 id FROM MaterialBatchMaster WHERE materialId = @MaterialId AND isActive = 1 ORDER BY id DESC);
                        IF @bId IS NOT NULL
                        BEGIN
                            UPDATE MaterialBatchMaster
                            SET qtyPurchased = ISNULL(qtyPurchased, 0) + @Qty,
                                qtyRemaining = ISNULL(qtyRemaining, 0) + @Qty
                            WHERE id = @bId;
                        END
                        ELSE
                        BEGIN
                            INSERT INTO MaterialBatchMaster (materialId, batchNumber, qtyPurchased, qtyRemaining, isActive)
                            VALUES (@MaterialId, 'DEFAULT', @Qty, @Qty, 1);
                            SET @bId = SCOPE_IDENTITY();
                        END
                        SELECT @bId;
                    ", bp, CommandType.Text);

                    if (bObj != null && int.TryParse(bObj.ToString(), out int resolvedId))
                    {
                        batchId = resolvedId;
                    }
                }

                SqlParameter[] ip = new SqlParameter[6];
                ip[0] = new SqlParameter("@PurchaseId", purchaseId);
                ip[1] = new SqlParameter("@MaterialId", materialId);
                ip[2] = new SqlParameter("@Qty", item.Qty);
                ip[3] = new SqlParameter("@Rate", item.Rate);
                ip[4] = new SqlParameter("@Total", item.Total);
                ip[5] = new SqlParameter("@BatchId", Utilities.DbValue(batchId));

                _client.ExecuteNonQuery(
                    @"INSERT INTO PurchaseItem (purchaseId,materialId,quantity,rate,total,materialBatchId)
              VALUES (@PurchaseId,@MaterialId,@Qty,@Rate,@Total,@BatchId)",
                    ip, CommandType.Text);
            }

            return new CreatePurchaseRes
            {
                Edid = EncryptionHelper.Encrypt(purchaseId.ToString()),
                InitialPaymentEdid = initialPaymentEdid,
                InitialPaymentTransactionNumber = initialPaymentTransactionNumber
            };
        }
        public bool UpdateStatus(string edid, string status)
        {
            SqlParameter[] p = new SqlParameter[2];
            p[0] = new SqlParameter("@Id",     EncryptionHelper.Decrypt(edid));
            p[1] = new SqlParameter("@Status", status);
            return _client.ExecuteNonQuery("UPDATE PurchaseMaster SET status=@Status WHERE id=@Id", p, CommandType.Text) > 0;
        }

        public List<PurchasePaymentItem> GetPayments(string purchaseEdid)
        {
            EnsurePurchasePaymentTable();
            int purchaseId = EncryptionHelper.Decrypt<int>(purchaseEdid);
            SqlParameter[] p = { new SqlParameter("@PurchaseId", purchaseId) };
            string query = @"SELECT id, transactionNumber, amount, paymentMethod, paymentType, status, paymentReference, remarks, paymentDate
                             FROM PurchasePayment
                             WHERE purchaseId = @PurchaseId
                             ORDER BY paymentDate DESC, id DESC";

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];
            List<PurchasePaymentItem> payments = new List<PurchasePaymentItem>();
            foreach (DataRow row in dt.Rows)
            {
                payments.Add(new PurchasePaymentItem
                {
                    Edid = EncryptionHelper.Encrypt(row.Field<int>("id").ToString()),
                    TransactionNumber = row.Field<string>("transactionNumber") ?? string.Empty,
                    Amount = row.Field<decimal>("amount"),
                    PaymentMethod = row.Field<string>("paymentMethod") ?? "Cash",
                    PaymentType = Utilities.ParseEnum<OrderPaymentType>(row.Field<string>("paymentType")) ?? OrderPaymentType.Advance,
                    Status = Utilities.ParseEnum<OrderPaymentStatus>(row.Field<string>("status")) ?? OrderPaymentStatus.Success,
                    PaymentReference = row.Field<string?>("paymentReference"),
                    Remarks = row.Field<string?>("remarks"),
                    PaymentDate = row.Field<DateTime>("paymentDate")
                });
            }

            return payments;
        }

        public int RecordPayment(string purchaseEdid, string supplierEdid, PayPurchaseDueReq req, string newStatus, decimal paidAmountDelta, string transactionNumber)
        {
            EnsurePurchasePaymentTable();
            string purchaseId = EncryptionHelper.Decrypt(purchaseEdid);
            string supplierId = EncryptionHelper.Decrypt(supplierEdid);
            DateTime paymentDate = req.PaymentDate ?? DateTime.UtcNow;
            SqlParameter[] p = new SqlParameter[12];
            p[0] = new SqlParameter("@PurchaseId", purchaseId);
            p[1] = new SqlParameter("@SupplierId", supplierId);
            p[2] = new SqlParameter("@Amount", req.Amount);
            p[3] = new SqlParameter("@Status", newStatus);
            p[4] = new SqlParameter("@PaidAmountDelta", paidAmountDelta);
            p[5] = new SqlParameter("@TransactionNumber", transactionNumber);
            p[6] = new SqlParameter("@PaymentMethod", req.PaymentMethod.ToString());
            p[7] = new SqlParameter("@PaymentType", req.PaymentType.ToString());
            p[8] = new SqlParameter("@PaymentStatus", req.Status.ToString());
            p[9] = new SqlParameter("@PaymentReference", Utilities.DbValue(req.PaymentReference));
            p[10] = new SqlParameter("@Remarks", Utilities.DbValue(req.Remarks));
            p[11] = new SqlParameter("@PaymentDate", paymentDate);

            string query = @"BEGIN TRY
                                BEGIN TRANSACTION;
                                UPDATE PurchaseMaster SET paidAmount = paidAmount + @PaidAmountDelta, status = @Status WHERE id = @PurchaseId;
                                UPDATE SupplierMaster SET balance = ISNULL(balance, 0) - @PaidAmountDelta WHERE id = @SupplierId;
                                INSERT INTO PurchasePayment (purchaseId, transactionNumber, amount, paymentMethod, paymentType, status, paymentReference, remarks, paymentDate)
                                VALUES (@PurchaseId, @TransactionNumber, @Amount, @PaymentMethod, @PaymentType, @PaymentStatus, @PaymentReference, @Remarks, @PaymentDate);
                                SELECT CAST(SCOPE_IDENTITY() AS INT);
                                COMMIT TRANSACTION;
                            END TRY
                            BEGIN CATCH
                                IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                                THROW;
                            END CATCH;";

            return _client.ExecuteScalar<int>(query, p, CommandType.Text);
        }

        private int InsertPaymentHistory(int purchaseId, decimal amount, string transactionNumber, string paymentMethod, OrderPaymentType paymentType, OrderPaymentStatus status, string? paymentReference, string? remarks, DateTime paymentDate)
        {
            SqlParameter[] p =
            {
                new SqlParameter("@PurchaseId", purchaseId),
                new SqlParameter("@TransactionNumber", transactionNumber),
                new SqlParameter("@Amount", amount),
                new SqlParameter("@PaymentMethod", string.IsNullOrWhiteSpace(paymentMethod) ? "Cash" : paymentMethod),
                new SqlParameter("@PaymentType", paymentType.ToString()),
                new SqlParameter("@Status", status.ToString()),
                new SqlParameter("@PaymentReference", Utilities.DbValue(paymentReference)),
                new SqlParameter("@Remarks", Utilities.DbValue(remarks)),
                new SqlParameter("@PaymentDate", paymentDate)
            };

            const string query = @"INSERT INTO PurchasePayment (purchaseId, transactionNumber, amount, paymentMethod, paymentType, status, paymentReference, remarks, paymentDate)
                                   OUTPUT INSERTED.id
                                   VALUES (@PurchaseId, @TransactionNumber, @Amount, @PaymentMethod, @PaymentType, @Status, @PaymentReference, @Remarks, @PaymentDate)";
            return _client.ExecuteScalar<int>(query, p, CommandType.Text);
        }

        private void EnsurePurchasePaymentTable()
        {
            const string query = @"IF OBJECT_ID(N'dbo.PurchasePayment', N'U') IS NULL
                                  BEGIN
                                      CREATE TABLE dbo.PurchasePayment
                                      (
                                          id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                          purchaseId INT NOT NULL,
                                          transactionNumber NVARCHAR(50) NULL,
                                          amount DECIMAL(18,2) NOT NULL,
                                          paymentMethod NVARCHAR(50) NOT NULL CONSTRAINT DF_PurchasePayment_Method DEFAULT 'Cash',
                                          paymentType NVARCHAR(30) NOT NULL CONSTRAINT DF_PurchasePayment_Type DEFAULT 'Advance',
                                          status NVARCHAR(30) NOT NULL CONSTRAINT DF_PurchasePayment_Status DEFAULT 'Success',
                                          paymentReference NVARCHAR(150) NULL,
                                          remarks NVARCHAR(500) NULL,
                                          paymentDate DATETIME2 NOT NULL CONSTRAINT DF_PurchasePayment_Date DEFAULT SYSUTCDATETIME()
                                      );
                                      CREATE INDEX IX_PurchasePayment_PurchaseId_PaymentDate
                                          ON dbo.PurchasePayment (purchaseId, paymentDate DESC, id DESC);
                                  END";
            _client.ExecuteNonQuery(query, null, CommandType.Text);

                const string ensureColumnsQuery = @"IF COL_LENGTH(N'dbo.PurchasePayment', N'transactionNumber') IS NULL
                                                                  ALTER TABLE dbo.PurchasePayment ADD transactionNumber NVARCHAR(50) NULL;
                                                              IF COL_LENGTH(N'dbo.PurchasePayment', N'paymentType') IS NULL
                                                                  ALTER TABLE dbo.PurchasePayment ADD paymentType NVARCHAR(30) NOT NULL CONSTRAINT DF_PurchasePayment_Type_Migration DEFAULT 'Advance';
                                                              IF COL_LENGTH(N'dbo.PurchasePayment', N'status') IS NULL
                                                                  ALTER TABLE dbo.PurchasePayment ADD status NVARCHAR(30) NOT NULL CONSTRAINT DF_PurchasePayment_Status_Migration DEFAULT 'Success';
                                                              UPDATE dbo.PurchasePayment
                                                              SET transactionNumber = CONCAT('PUR-', id)
                                                              WHERE transactionNumber IS NULL";
                _client.ExecuteNonQuery(ensureColumnsQuery, null, CommandType.Text);

                const string backfillQuery = @"INSERT INTO dbo.PurchasePayment (purchaseId, transactionNumber, amount, paymentMethod, paymentType, status, remarks, paymentDate)
                                                        SELECT p.id, CONCAT('PUR-HIST-', p.id), p.paidAmount, 'Historical', 'Advance', 'Success', 'Imported from existing purchase paid amount', p.date
                                          FROM dbo.PurchaseMaster p
                                          WHERE p.paidAmount > 0
                                            AND NOT EXISTS (
                                                SELECT 1 FROM dbo.PurchasePayment pp WHERE pp.purchaseId = p.id
                                            )";
            _client.ExecuteNonQuery(backfillQuery, null, CommandType.Text);
        }

        // ── Purchase Items ────────────────────────────────────────────────────

        public GetPurchaseItemsRes GetItems(GetPurchaseItemsReq req)
        {
            string purchaseId = EncryptionHelper.Decrypt(req.PurchaseEdid);
            int pageNumber = req.PageNumber > 0 ? req.PageNumber : 1;
            int pageSize = req.PageSize > 0 ? req.PageSize : 10;

            SqlParameter[] countParams = { new SqlParameter("@PurchaseId", purchaseId) };

            string countQuery = "SELECT COUNT(*) FROM PurchaseItem WHERE purchaseId = @PurchaseId";
            int totalCount = _client.ExecuteScalar<int>(countQuery, countParams, CommandType.Text);

            SqlParameter[] p = {
        new SqlParameter("@PurchaseId", purchaseId),
        new SqlParameter("@Offset", (pageNumber - 1) * pageSize),
        new SqlParameter("@PageSize", pageSize)
    };

            string query = @"
        SELECT p.purchaseId, p.materialId, rm.name AS materialName,
               rc.name AS categoryName, u.symbol AS unitSymbol,
               p.quantity AS qty, p.rate, p.total,
               mb.batchNumber AS batchNumber
        FROM PurchaseItem p
        LEFT JOIN MaterialBatchMaster mb ON p.materialBatchId = mb.id
        INNER JOIN RawMaterialMaster rm ON p.materialId = rm.id
        LEFT JOIN RawMaterialCategoryMaster rc ON rm.categoryId = rc.id
        LEFT JOIN UnitMaster u ON rm.unitId = u.id
        WHERE p.purchaseId = @PurchaseId
        ORDER BY p.id ASC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            GetPurchaseItemsRes res = new GetPurchaseItemsRes
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            foreach (DataRow row in dt.Rows)
            {
                res.Items.Add(new PurchaseItemLine
                {
                    PurchaseEdid = EncryptionHelper.Encrypt(row.Field<int>("purchaseId").ToString()),
                    MaterialEdid = EncryptionHelper.Encrypt(row.Field<int>("materialId").ToString()),
                    MaterialName = row.Field<string>("materialName") ?? "",
                    CategoryName = row.Field<string>("categoryName") ?? "",
                    UnitSymbol = row.Field<string>("unitSymbol") ?? "",
                    Qty = row.Field<decimal>("qty"),
                    Rate = row.Field<decimal>("rate"),
                    Total = row.Field<decimal>("total"),
                    BatchNumber = row.Field<string?>("batchNumber")
                });
            }

            return res;
        }

    }
}
