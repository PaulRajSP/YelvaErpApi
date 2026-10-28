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
        CreatePurchaseRes   Create(CreatePurchaseReq req);
        bool RecordPayment(string purchaseEdid, string supplierEdid, decimal amount, string newStatus);
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

            string query = @"SELECT COUNT(*) OVER() AS TotalRecords, p.id, p.docId, p.supplierId,
                            s.name AS supplierName, p.date, p.totalAmount, p.status,
                            p.paidAmount, (p.totalAmount - p.paidAmount) AS balance,
                            a.originalName AS documentOriginalName
                     FROM PurchaseMaster p
                     INNER JOIN SupplierMaster s ON p.supplierId = s.id
                     LEFT JOIN AttachmentMaster a ON p.docId = a.id
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
                int? docId = (dt.Columns.Contains("docId") && !row.IsNull("docId")) ? Convert.ToInt32(row["docId"]) : (int?)null;
                string? docEdid = docId.HasValue ? EncryptionHelper.Encrypt(docId.Value.ToString()) : null;
                string? docOriginalName = (dt.Columns.Contains("documentOriginalName") && !row.IsNull("documentOriginalName")) ? row.Field<string>("documentOriginalName") : null;

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
                    AttachmentData = !string.IsNullOrWhiteSpace(docEdid) ? new DocumentModel
                    {
                        DocumentEdid = docEdid,
                        DocumentOriginalName = docOriginalName
                    } : null
                });
            }

            return res;
        }

        public GetPurchaseListRes GetBySupplier(GetPurchasesBySupplierReq req)
        {
            int pageNumber = req.PageNumber > 0 ? req.PageNumber : 1;
            int pageSize = req.PageSize > 0 ? req.PageSize : 10;
            int offset = (pageNumber - 1) * pageSize;

            string query = @"SELECT COUNT(*) OVER() AS TotalRecords, p.id, p.docId, p.supplierId, s.name AS supplierName,
                            p.date, p.totalAmount, p.status, p.paidAmount,
                            (p.totalAmount - p.paidAmount) AS balance,
                            a.originalName AS documentOriginalName
                     FROM PurchaseMaster p
                     INNER JOIN SupplierMaster s ON p.supplierId = s.id
                     LEFT JOIN AttachmentMaster a ON p.docId = a.id
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
                int? docId = (dt.Columns.Contains("docId") && !row.IsNull("docId")) ? Convert.ToInt32(row["docId"]) : (int?)null;
                string? docEdid = docId.HasValue ? EncryptionHelper.Encrypt(docId.Value.ToString()) : null;
                string? docOriginalName = (dt.Columns.Contains("documentOriginalName") && !row.IsNull("documentOriginalName")) ? row.Field<string>("documentOriginalName") : null;

                res.Items.Add(new PurchaseListItem
                {
                    Edid = EncryptionHelper.Encrypt(row.Field<int>("id").ToString()),
                    PublicId = row.Field<int>("id").ToString(),
                    Date = row.Field<DateTime>("date"),
                    TotalAmount = row.Field<decimal>("totalAmount"),
                    PaidAmount = row.Field<decimal>("paidAmount"),
                    Balance = Convert.ToDecimal(row["balance"]),
                    Status = (Convert.ToDecimal(row["balance"]) <= 0 && row.Field<decimal>("totalAmount") > 0) ? "PAID" : row.Field<string>("status")!,
                    AttachmentData = !string.IsNullOrWhiteSpace(docEdid) ? new DocumentModel
                    {
                        DocumentEdid = docEdid,
                        DocumentOriginalName = docOriginalName
                    } : null
                });
            }

            return res;
        }

        public GetPurchaseByIdRes? GetById(string edid)
        {
            SqlParameter[] p = { new SqlParameter("@Id", EncryptionHelper.Decrypt(edid)) };

            string query = @"SELECT p.id, p.docId, p.supplierId, s.name AS supplierName, p.date,
                            p.totalAmount, p.status, p.paidAmount,
                            (p.totalAmount - p.paidAmount) AS balance,
                            a.originalName AS documentOriginalName
                     FROM PurchaseMaster p
                     INNER JOIN SupplierMaster s ON p.supplierId = s.id
                     LEFT JOIN AttachmentMaster a ON p.docId = a.id
                     WHERE p.id = @Id";

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            int? docId = (dt.Columns.Contains("docId") && !row.IsNull("docId")) ? Convert.ToInt32(row["docId"]) : (int?)null;
            string? docEdid = docId.HasValue ? EncryptionHelper.Encrypt(docId.Value.ToString()) : null;
            string? docOriginalName = (dt.Columns.Contains("documentOriginalName") && !row.IsNull("documentOriginalName")) ? row.Field<string>("documentOriginalName") : null;

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
                AttachmentData = !string.IsNullOrWhiteSpace(docEdid) ? new DocumentModel
                {
                    DocumentEdid = docEdid,
                    DocumentOriginalName = docOriginalName
                } : null
            };
        }

        public CreatePurchaseRes Create(CreatePurchaseReq req)
        {
            string supplierId = EncryptionHelper.Decrypt(req.Purchase.SupplierEdid);

            req.Purchase.Balance = req.Purchase.TotalAmount - req.Purchase.PaidAmount;

            int? docId = null;
            string? docEdid = req.Purchase.AttachmentData?.DocumentEdid;
            if (!string.IsNullOrWhiteSpace(docEdid))
            {
                string decrypted = EncryptionHelper.Decrypt(docEdid);
                if (int.TryParse(decrypted, out int parsedId))
                    docId = parsedId;
                else if (int.TryParse(docEdid, out int rawId))
                    docId = rawId;
            }

            SqlParameter[] p = new SqlParameter[6];
            p[0] = new SqlParameter("@SupplierId", supplierId);
            p[1] = new SqlParameter("@TotalAmount", req.Purchase.TotalAmount);
            p[2] = new SqlParameter("@Status", req.Purchase.Status.ToString().ToUpper());
            p[3] = new SqlParameter("@Date", req.Purchase.Date);
            p[4] = new SqlParameter("@PaidAmount", req.Purchase.PaidAmount);
            p[5] = new SqlParameter("@DocId", (object?)docId ?? DBNull.Value);

            string query = @"INSERT INTO PurchaseMaster (supplierId,date,totalAmount,status,paidAmount,docId)
                     OUTPUT INSERTED.Id
                     VALUES (@SupplierId,@Date,@TotalAmount,@Status,@PaidAmount,@DocId)";

            int purchaseId = _client.ExecuteScalar<int>(query, p, CommandType.Text);

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

            return new CreatePurchaseRes { Edid = EncryptionHelper.Encrypt(purchaseId.ToString()) };
        }
        public bool UpdateStatus(string edid, string status)
        {
            SqlParameter[] p = new SqlParameter[2];
            p[0] = new SqlParameter("@Id",     EncryptionHelper.Decrypt(edid));
            p[1] = new SqlParameter("@Status", status);
            return _client.ExecuteNonQuery("UPDATE PurchaseMaster SET status=@Status WHERE id=@Id", p, CommandType.Text) > 0;
        }

        public bool RecordPayment(string purchaseEdid, string supplierEdid, decimal amount, string newStatus)
        {
            string purchaseId = EncryptionHelper.Decrypt(purchaseEdid);
            string supplierId = EncryptionHelper.Decrypt(supplierEdid);

            SqlParameter[] p = new SqlParameter[4];
            p[0] = new SqlParameter("@PurchaseId", purchaseId);
            p[1] = new SqlParameter("@SupplierId", supplierId);
            p[2] = new SqlParameter("@Amount", amount);
            p[3] = new SqlParameter("@Status", newStatus);

            string query = @"UPDATE PurchaseMaster SET paidAmount = paidAmount + @Amount, status = @Status WHERE id = @PurchaseId;
                            UPDATE SupplierMaster SET balance = ISNULL(balance, 0) - @Amount WHERE id = @SupplierId;";

            return _client.ExecuteNonQuery(query, p, CommandType.Text) > 0;
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
