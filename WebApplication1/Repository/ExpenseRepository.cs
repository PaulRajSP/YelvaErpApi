using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Repository
{
    public interface IExpenseRepository
    {
        PageResult<ExpenseItemDto> GetAll(GetExpenseListReq req);
        ExpenseItemDto? GetById(string edid);
        string Create(CreateExpenseReq req);
        bool Update(string edid, UpdateExpenseReq req);
        bool Delete(string edid);
        ExpenseSummaryRes GetSummary(DateTime? fromDate, DateTime? toDate);
    }

    public class ExpenseRepository : IExpenseRepository
    {
        private readonly DbClient _client;

        public ExpenseRepository(DbClient client)
        {
            _client = client;
        }

        public PageResult<ExpenseItemDto> GetAll(GetExpenseListReq req)
        {
            int pageNumber = req.Pagination.PageNumber > 0 ? req.Pagination.PageNumber : 1;
            int pageSize   = req.Pagination.PageSize   > 0 ? req.Pagination.PageSize   : 10;
            int offset = (pageNumber - 1) * pageSize;

            string query = @"
                SELECT COUNT(*) OVER() AS TotalRecords,
                       id, expenseNumber, title, category, amount, expenseDate,
                       paymentMode, paidTo, referenceNumber, docEdid, status, remarks,
                       recordedBy, transactionMasterId, createdAt, updatedAt
                FROM ExpenseMaster
                WHERE (@FromDate IS NULL OR expenseDate >= @FromDate)
                  AND (@ToDate IS NULL OR expenseDate <= @ToDate)
                  AND (@Category IS NULL OR category = @Category)
                  AND (@Status IS NULL OR status = @Status)
                  AND (@PaymentMode IS NULL OR paymentMode = @PaymentMode)
                  AND (
                      @SearchKeyword IS NULL
                      OR title LIKE '%' + @SearchKeyword + '%'
                      OR expenseNumber LIKE '%' + @SearchKeyword + '%'
                      OR paidTo LIKE '%' + @SearchKeyword + '%'
                      OR referenceNumber LIKE '%' + @SearchKeyword + '%'
                  )
                ORDER BY expenseDate DESC, id DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            SqlParameter[] p = new SqlParameter[8];
            p[0] = new SqlParameter("@FromDate", req.FromDate.HasValue ? (object)req.FromDate.Value : DBNull.Value);
            p[1] = new SqlParameter("@ToDate", req.ToDate.HasValue ? (object)req.ToDate.Value : DBNull.Value);
            p[2] = new SqlParameter("@Category", string.IsNullOrWhiteSpace(req.Category) ? DBNull.Value : req.Category);
            p[3] = new SqlParameter("@Status", string.IsNullOrWhiteSpace(req.Status) ? DBNull.Value : req.Status);
            p[4] = new SqlParameter("@PaymentMode", string.IsNullOrWhiteSpace(req.PaymentMode) ? DBNull.Value : req.PaymentMode);
            p[5] = new SqlParameter("@SearchKeyword", string.IsNullOrWhiteSpace(req.SearchKeyword) ? DBNull.Value : req.SearchKeyword.Trim());
            p[6] = new SqlParameter("@Offset", offset);
            p[7] = new SqlParameter("@PageSize", pageSize);

            DataSet ds = _client.ExecuteAdapter(query, p, CommandType.Text);

            var res = new PageResult<ExpenseItemDto>
            {
                TotalCount = 0,
                PageNumber = pageNumber,
                PageSize   = pageSize
            };

            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                res.TotalCount = Convert.ToInt32(ds.Tables[0].Rows[0]["TotalRecords"]);

                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    res.Items.Add(MapRowToDto(row));
                }
            }

            return res;
        }

        public ExpenseItemDto? GetById(string edid)
        {
            int id = ResolveId(edid);
            if (id <= 0) return null;

            SqlParameter[] p = { new SqlParameter("@Id", id) };
            string query = @"
                SELECT id, expenseNumber, title, category, amount, expenseDate,
                       paymentMode, paidTo, referenceNumber, docEdid, status, remarks,
                       recordedBy, transactionMasterId, createdAt, updatedAt
                FROM ExpenseMaster
                WHERE id = @Id;";

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];
            if (dt.Rows.Count == 0) return null;

            return MapRowToDto(dt.Rows[0]);
        }

        public string Create(CreateExpenseReq req)
        {
            string expenseNumber = Utilities.GeneratePublicId("EXP", 5); //$"EXP-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
            DateTime expenseDate = req.ExpenseDate ?? DateTime.UtcNow;
            bool isPaid = string.Equals(req.Status, "Paid", StringComparison.OrdinalIgnoreCase);

            string query = @"
                INSERT INTO ExpenseMaster (
                    expenseNumber, title, category, amount, expenseDate,
                    paymentMode, paidTo, referenceNumber, status,
                    remarks, recordedBy, createdAt, updatedAt
                )
                OUTPUT INSERTED.id
                VALUES (
                    @ExpenseNumber, @Title, @Category, @Amount, @ExpenseDate,
                    @PaymentMode, @PaidTo, @ReferenceNumber, @Status,
                    @Remarks, @RecordedBy, GETUTCDATE(), GETUTCDATE()
                );";

            SqlParameter[] p = new SqlParameter[11];
            p[0]  = new SqlParameter("@ExpenseNumber", expenseNumber);
            p[1]  = new SqlParameter("@Title", req.Title ?? string.Empty);
            p[2]  = new SqlParameter("@Category", req.Category ?? string.Empty);
            p[3]  = new SqlParameter("@Amount", req.Amount);
            p[4]  = new SqlParameter("@ExpenseDate", expenseDate);
            p[5]  = new SqlParameter("@PaymentMode", req.PaymentMode ?? string.Empty);
            p[6]  = new SqlParameter("@PaidTo", Utilities.DbValue(req.PaidTo));
            p[7]  = new SqlParameter("@ReferenceNumber", Utilities.DbValue(req.ReferenceNumber));
            p[8]  = new SqlParameter("@Status", req.Status ?? string.Empty);
            p[9]  = new SqlParameter("@Remarks", Utilities.DbValue(req.Remarks));
            p[10] = new SqlParameter("@RecordedBy", Utilities.DbValue(req.RecordedBy));

            int insertedId = _client.ExecuteScalar<int>(query, p, CommandType.Text);

            // Record into PaymentTransactionMaster (Financial Ledger) with Category Expense
            RecordPaymentTransactionLedger(insertedId, expenseNumber, req, isPaid);

            return EncryptionHelper.Encrypt(insertedId.ToString());
        }

        public bool Update(string edid, UpdateExpenseReq req)
        {
            int id = ResolveId(edid);
            if (id <= 0) return false;

            SqlParameter[] pId = { new SqlParameter("@Id", id) };
            DataTable dtExisting = _client.ExecuteAdapter("SELECT expenseNumber, status FROM ExpenseMaster WHERE id = @Id;", pId, CommandType.Text).Tables[0];
            if (dtExisting.Rows.Count == 0) return false;

            DataRow existingRow = dtExisting.Rows[0];
            string expenseNumber = existingRow["expenseNumber"]?.ToString() ?? string.Empty;

            DateTime expenseDate = req.ExpenseDate ?? DateTime.UtcNow;
            bool isPaid = string.Equals(req.Status, "Paid", StringComparison.OrdinalIgnoreCase);

            string txnDesc = $"Expense: {req.Title} ({req.Category})";
            if (!string.IsNullOrWhiteSpace(req.PaidTo))
                txnDesc += $" - {req.PaidTo}";
            if (!string.IsNullOrWhiteSpace(req.PaymentMode))
                txnDesc += $" via {req.PaymentMode}";
            if (!string.IsNullOrWhiteSpace(req.ReferenceNumber))
                txnDesc += $" (Ref: {req.ReferenceNumber})";
            string pRemark = txnDesc.Length > 490 ? txnDesc.Substring(0, 490) : txnDesc;

            string query = @"
                SET XACT_ABORT ON;
                BEGIN TRY
                    BEGIN TRANSACTION;
                    DECLARE @TransactionMasterId INT;

                    UPDATE ExpenseMaster
                    SET title = @Title,
                        category = @Category,
                        amount = @Amount,
                        expenseDate = @ExpenseDate,
                        paymentMode = @PaymentMode,
                        paidTo = @PaidTo,
                        referenceNumber = @ReferenceNumber,
                        status = @Status,
                        remarks = @Remarks,
                        recordedBy = COALESCE(@RecordedBy, recordedBy),
                        updatedAt = GETUTCDATE()
                    WHERE id = @Id;

                    IF @@ROWCOUNT = 0
                        THROW 50001, 'Expense record not found.', 1;

                    IF EXISTS (SELECT 1 FROM PaymentTransactionMaster WHERE transactionNumber = @ExpenseNumber)
                    BEGIN
                        SELECT TOP 1 @TransactionMasterId = id
                        FROM PaymentTransactionMaster
                        WHERE transactionNumber = @ExpenseNumber
                        ORDER BY id DESC;

                        UPDATE PaymentTransactionMaster
                        SET remark = @Remark,
                            amount = @Amount,
                            status = @TransactionStatus,
                            category = @TransactionCategory,
                            isActive = @IsActive
                        WHERE id = @TransactionMasterId;
                    END
                    ELSE
                    BEGIN
                        DECLARE @InsertedTransaction TABLE (id INT);

                        INSERT INTO PaymentTransactionMaster
                            (transactionNumber, remark, amount, status, category, isActive, balance, createdAt)
                        OUTPUT INSERTED.id INTO @InsertedTransaction(id)
                        VALUES
                            (@ExpenseNumber, @Remark, @Amount, @TransactionStatus, @TransactionCategory, @IsActive, 0, SYSUTCDATETIME());

                        SELECT @TransactionMasterId = id FROM @InsertedTransaction;
                    END;

                    UPDATE ExpenseMaster
                    SET transactionMasterId = @TransactionMasterId
                    WHERE id = @Id;

                    ;WITH RunningBalances AS
                    (
                        SELECT id,
                            SUM(CASE
                                WHEN isActive = 1 AND UPPER(status) = 'DEBIT' THEN -amount
                                WHEN isActive = 1 AND UPPER(status) = 'CREDIT' THEN amount
                                ELSE 0
                            END) OVER (ORDER BY id ROWS UNBOUNDED PRECEDING) AS calculatedBalance
                        FROM PaymentTransactionMaster
                    )
                    UPDATE ledger
                    SET balance = running.calculatedBalance
                    FROM PaymentTransactionMaster AS ledger
                    INNER JOIN RunningBalances AS running ON running.id = ledger.id;

                    COMMIT TRANSACTION;
                END TRY
                BEGIN CATCH
                    IF @@TRANCOUNT > 0
                        ROLLBACK TRANSACTION;
                    THROW;
                END CATCH;";

            SqlParameter[] p = new SqlParameter[16];
            p[0]  = new SqlParameter("@Id", id);
            p[1]  = new SqlParameter("@Title", req.Title ?? string.Empty);
            p[2]  = new SqlParameter("@Category", req.Category ?? string.Empty);
            p[3]  = new SqlParameter("@Amount", req.Amount);
            p[4]  = new SqlParameter("@ExpenseDate", expenseDate);
            p[5]  = new SqlParameter("@PaymentMode", req.PaymentMode ?? string.Empty);
            p[6]  = new SqlParameter("@PaidTo", Utilities.DbValue(req.PaidTo));
            p[7]  = new SqlParameter("@ReferenceNumber", Utilities.DbValue(req.ReferenceNumber));
            p[8]  = new SqlParameter("@Status", req.Status ?? string.Empty);
            p[9]  = new SqlParameter("@Remarks", Utilities.DbValue(req.Remarks));
            p[10] = new SqlParameter("@RecordedBy", Utilities.DbValue(req.RecordedBy));
            p[11] = new SqlParameter("@ExpenseNumber", expenseNumber);
            p[12] = new SqlParameter("@Remark", pRemark);
            p[13] = new SqlParameter("@TransactionStatus", PaymentType.Debit.ToString());
            p[14] = new SqlParameter("@TransactionCategory", TransactionCategory.Expense.ToString());
            p[15] = new SqlParameter("@IsActive", isPaid);

            _client.ExecuteNonQuery(query, p, CommandType.Text);
            return true;
        }

        public bool Delete(string edid)
        {
            int id = ResolveId(edid);
            if (id <= 0) return false;

            SqlParameter[] pId = { new SqlParameter("@Id", id) };
            DataTable dt = _client.ExecuteAdapter("SELECT expenseNumber FROM ExpenseMaster WHERE id = @Id;", pId, CommandType.Text).Tables[0];
            if (dt.Rows.Count > 0)
            {
                string expenseNumber = dt.Rows[0]["expenseNumber"]?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(expenseNumber))
                {
                    try
                    {
                        SqlParameter[] ptP = { new SqlParameter("@TransactionNumber", expenseNumber) };
                        _client.ExecuteNonQuery("DELETE FROM PaymentTransactionMaster WHERE transactionNumber = @TransactionNumber;", ptP, CommandType.Text);
                    }
                    catch { }
                }
            }

            SqlParameter[] p = { new SqlParameter("@Id", id) };
            return _client.ExecuteNonQuery("DELETE FROM ExpenseMaster WHERE id = @Id;", p, CommandType.Text) > 0;
        }
        
        private void RecordPaymentTransactionLedger(int expenseId, string expenseNumber, CreateExpenseReq req, bool isPaid)
        {
            string txnDesc = $"Expense: {req.Title} ({req.Category})";
            if (!string.IsNullOrWhiteSpace(req.PaidTo))
                txnDesc += $" - {req.PaidTo}";
            if (!string.IsNullOrWhiteSpace(req.PaymentMode))
                txnDesc += $" via {req.PaymentMode}";
            if (!string.IsNullOrWhiteSpace(req.ReferenceNumber))
                txnDesc += $" (Ref: {req.ReferenceNumber})";
            string pRemark = txnDesc.Length > 490 ? txnDesc.Substring(0, 490) : txnDesc;

            SqlParameter[] lp = new SqlParameter[7];
            lp[0] = new SqlParameter("@ExpenseId", expenseId);
            lp[1] = new SqlParameter("@TransactionNumber", expenseNumber);
            lp[2] = new SqlParameter("@Description", pRemark);
            lp[3] = new SqlParameter("@Amount", req.Amount);
            lp[4] = new SqlParameter("@Status", PaymentType.Debit.ToString());
            lp[5] = new SqlParameter("@Category", TransactionCategory.Expense.ToString());
            lp[6] = new SqlParameter("@IsActive", isPaid);

            string ledgerQuery = @"
                SET XACT_ABORT ON;
                BEGIN TRY
                    BEGIN TRANSACTION;
                    DECLARE @PrevBalance DECIMAL(18,2) = 0;
                    SELECT TOP 1 @PrevBalance = ISNULL(balance, 0) FROM PaymentTransactionMaster ORDER BY id DESC;

                    DECLARE @NewBalance DECIMAL(18,2);
                    IF @IsActive = 1
                        SET @NewBalance = @PrevBalance - @Amount;
                    ELSE
                        SET @NewBalance = @PrevBalance;

                    DECLARE @InsertedTransaction TABLE (id INT);
                    INSERT INTO PaymentTransactionMaster (transactionNumber, remark, amount, status, category, isActive, balance, createdAt)
                    OUTPUT INSERTED.id INTO @InsertedTransaction(id)
                    VALUES (@TransactionNumber, @Description, @Amount, @Status, @Category, @IsActive, @NewBalance, SYSUTCDATETIME());

                    UPDATE ExpenseMaster
                    SET transactionMasterId = (SELECT id FROM @InsertedTransaction)
                    WHERE id = @ExpenseId;

                    COMMIT TRANSACTION;
                END TRY
                BEGIN CATCH
                    IF @@TRANCOUNT > 0
                        ROLLBACK TRANSACTION;
                    THROW;
                END CATCH;";

            _client.ExecuteNonQuery(ledgerQuery, lp, CommandType.Text);
        }

        public ExpenseSummaryRes GetSummary(DateTime? fromDate, DateTime? toDate)
        {
            string query = @"
                SELECT
                    ISNULL(SUM(amount), 0) AS TotalExpenses,
                    ISNULL(SUM(CASE WHEN expenseDate >= DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1) THEN amount ELSE 0 END), 0) AS ThisMonthTotal,
                    ISNULL(SUM(CASE WHEN CAST(expenseDate AS DATE) = CAST(GETDATE() AS DATE) THEN amount ELSE 0 END), 0) AS TodayTotal,
                    COUNT(*) AS TotalCount
                FROM ExpenseMaster
                WHERE (@FromDate IS NULL OR expenseDate >= @FromDate)
                  AND (@ToDate IS NULL OR expenseDate <= @ToDate);

                SELECT category, ISNULL(SUM(amount), 0) AS TotalAmount, COUNT(*) AS Count
                FROM ExpenseMaster
                WHERE (@FromDate IS NULL OR expenseDate >= @FromDate)
                  AND (@ToDate IS NULL OR expenseDate <= @ToDate)
                GROUP BY category
                ORDER BY TotalAmount DESC;";

            SqlParameter[] p = new SqlParameter[2];
            p[0] = new SqlParameter("@FromDate", fromDate.HasValue ? (object)fromDate.Value : DBNull.Value);
            p[1] = new SqlParameter("@ToDate", toDate.HasValue ? (object)toDate.Value : DBNull.Value);

            DataSet ds = _client.ExecuteAdapter(query, p, CommandType.Text);

            var res = new ExpenseSummaryRes();

            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                var r0 = ds.Tables[0].Rows[0];
                res.TotalExpenses  = Convert.ToDecimal(r0["TotalExpenses"]);
                res.ThisMonthTotal = Convert.ToDecimal(r0["ThisMonthTotal"]);
                res.TodayTotal     = Convert.ToDecimal(r0["TodayTotal"]);
                res.TotalCount     = Convert.ToInt32(r0["TotalCount"]);
            }

            if (ds != null && ds.Tables.Count > 1)
            {
                foreach (DataRow row in ds.Tables[1].Rows)
                {
                    res.CategoryBreakdown.Add(new CategoryExpenseSummary
                    {
                        Category    = row["category"]?.ToString() ?? "",
                        TotalAmount = Convert.ToDecimal(row["TotalAmount"]),
                        Count       = Convert.ToInt32(row["Count"])
                    });
                }
            }

            return res;
        }

        private static int ResolveId(string edid)
        {
            if (string.IsNullOrWhiteSpace(edid)) return 0;
            string decrypted = EncryptionHelper.Decrypt(edid);
            if (int.TryParse(decrypted, out int id)) return id;
            if (int.TryParse(edid, out id)) return id;
            return 0;
        }

        private static ExpenseItemDto MapRowToDto(DataRow row)
        {
            return new ExpenseItemDto
            {
                Edid                  = EncryptionHelper.Encrypt(row["id"].ToString()!),
                ExpenseNumber         = row["expenseNumber"]?.ToString() ?? "",
                Title                 = row["title"]?.ToString() ?? "",
                Category              = row["category"]?.ToString() ?? "",
                Amount                = Convert.ToDecimal(row["amount"]),
                ExpenseDate           = Convert.ToDateTime(row["expenseDate"]),
                PaymentMode           = row["paymentMode"]?.ToString() ?? "",
                PaidTo                = row["paidTo"] == DBNull.Value ? null : row["paidTo"]?.ToString(),
                ReferenceNumber       = row["referenceNumber"] == DBNull.Value ? null : row["referenceNumber"]?.ToString(),
                LegacyDocEdid         = row["docEdid"] == DBNull.Value ? null : row["docEdid"]?.ToString(),
                Status                = row["status"]?.ToString() ?? "",
                Remarks               = row["remarks"] == DBNull.Value ? null : row["remarks"]?.ToString(),
                RecordedBy            = row["recordedBy"] == DBNull.Value ? null : row["recordedBy"]?.ToString(),
                TransactionMasterId   = row.Table.Columns.Contains("transactionMasterId") && row["transactionMasterId"] != DBNull.Value ? Convert.ToInt32(row["transactionMasterId"]) : null,
                TransactionMasterEdid = (row.Table.Columns.Contains("transactionMasterId") && row["transactionMasterId"] != DBNull.Value) ? EncryptionHelper.Encrypt(row["transactionMasterId"].ToString()!) : null,
                CreatedAt             = Convert.ToDateTime(row["createdAt"]),
                UpdatedAt             = Convert.ToDateTime(row["updatedAt"])
            };
        }
    }
}
