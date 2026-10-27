using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Repository
{
    public interface IPaymentTransactionRepository
    {
        PageResult<PaymentTransactionListItem> GetAll(GetPaymentTransactionListReq req);
        CreatePaymentTransactionRes Create(CreatePaymentTransactionReq req);
        decimal GetBalance();
    }

    public class PaymentTransactionRepository : IPaymentTransactionRepository
    {
        private readonly DbClient _client;
        public PaymentTransactionRepository(DbClient client)
        {
            _client = client;
        }

        public CreatePaymentTransactionRes Create(CreatePaymentTransactionReq req)
        {
            SqlParameter[] p = new SqlParameter[6];
            p[0] = new SqlParameter("@TransactionNumber", Utilities.DbValue(req.TransactionNumber));
            p[1] = new SqlParameter("@Description", Utilities.DbValue(req.Description));
            p[2] = new SqlParameter("@Amount", req.Amount);
            p[3] = new SqlParameter("@Status", req.Status.ToString());
            p[4] = new SqlParameter("@Category", req.Category.ToString());
            p[5] = new SqlParameter("@IsActive", req.IsActive);

            string query = @"DECLARE @PrevBalance DECIMAL(18,2) = 0;
                           SELECT TOP 1 @PrevBalance = ISNULL(balance, 0) FROM PaymentTransactionMaster ORDER BY id DESC;
                
                           DECLARE @NewBalance DECIMAL(18,2);
                           IF @IsActive = 1
                           BEGIN
                               IF UPPER(@Status) = 'DEBIT'
                                    SET @NewBalance = @PrevBalance - @Amount;
                               ELSE
                                    SET @NewBalance = @PrevBalance + @Amount;
                           END
                           ELSE
                           BEGIN
                               SET @NewBalance = @PrevBalance;
                           END

                           INSERT INTO PaymentTransactionMaster (transactionNumber, remark, amount, status, category, isActive, balance, createdAt) 
                           VALUES (@TransactionNumber, @Description, @Amount, @Status, @Category, @IsActive, @NewBalance, SYSUTCDATETIME());
                
                           SELECT SCOPE_IDENTITY();";

            object idObj = _client.ExecuteScalar<object>(query, p, CommandType.Text);
            string id = idObj?.ToString() ?? "0";

            return new CreatePaymentTransactionRes { Edid = EncryptionHelper.Encrypt(id) };
        }

        public PageResult<PaymentTransactionListItem> GetAll(GetPaymentTransactionListReq req)
        {
            int pageNumber = req.Pagination.PageNumber > 0 ? req.Pagination.PageNumber : 1;
            int pageSize   = req.Pagination.PageSize   > 0 ? req.Pagination.PageSize   : 10;
            int offset = (pageNumber - 1) * pageSize;

            string query = @"SELECT COUNT(*) OVER() AS TotalCount, id, transactionNumber, remark AS description, amount, status, category, isActive, balance, createdAt
                           FROM PaymentTransactionMaster 
                           WHERE (@Status IS NULL OR UPPER(status) = UPPER(@Status))
                             AND (@Category IS NULL OR UPPER(category) = UPPER(@Category))
                             AND (@IsActive IS NULL OR isActive = @IsActive)
                             AND (@FromDate IS NULL OR createdAt >= @FromDate)
                             AND (@ToDate IS NULL OR createdAt <= @ToDate)
                             AND (@SearchKeyword IS NULL 
                                  OR transactionNumber LIKE '%' + @SearchKeyword + '%' 
                                  OR remark LIKE '%' + @SearchKeyword + '%'
                                  OR status LIKE '%' + @SearchKeyword + '%'
                                  OR category LIKE '%' + @SearchKeyword + '%')
                           ORDER BY id DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            SqlParameter[] p = new SqlParameter[8];
            p[0] = new SqlParameter("@Offset", offset);
            p[1] = new SqlParameter("@PageSize", pageSize);
            p[2] = new SqlParameter("@Status", req.Status.HasValue ? (object)req.Status.Value.ToString() : DBNull.Value);
            p[3] = new SqlParameter("@FromDate", req.FromDate.HasValue ? (object)req.FromDate.Value : DBNull.Value);
            p[4] = new SqlParameter("@ToDate", req.ToDate.HasValue ? (object)req.ToDate.Value : DBNull.Value);
            p[5] = new SqlParameter("@SearchKeyword", Utilities.DbValue(req.SearchKeyword));
            p[6] = new SqlParameter("@Category", req.Category.HasValue ? (object)req.Category.Value.ToString() : DBNull.Value);
            p[7] = new SqlParameter("@IsActive", req.IsActive.HasValue ? (object)req.IsActive.Value : DBNull.Value);

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            PageResult<PaymentTransactionListItem> res = new PageResult<PaymentTransactionListItem>
            {
                TotalCount = dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["TotalCount"]) : 0,
                PageNumber = pageNumber,
                PageSize   = pageSize
            };

            foreach (DataRow row in dt.Rows)
            {
                res.Items.Add(new PaymentTransactionListItem
                {
                    Edid = EncryptionHelper.Encrypt(row["id"].ToString() ?? "0"),
                    TransactionNumber = row["transactionNumber"].ToString() ?? "",
                    Description = row["description"].ToString() ?? "",
                    Amount = Convert.ToDecimal(row["amount"]),
                    Status = Utilities.ParseEnum<PaymentType>(row["status"].ToString()),
                    Category = row.Table.Columns.Contains("category") && row["category"] != DBNull.Value 
                        ? Utilities.ParseEnum<TransactionCategory>(row["category"]?.ToString())
                        : null,
                    IsActive = !row.Table.Columns.Contains("isActive") || row["isActive"] == DBNull.Value || Convert.ToBoolean(row["isActive"]),
                    Balance = Convert.ToDecimal(row["balance"]),
                    CreatedAt = Convert.ToDateTime(row["createdAt"])
                });
            }

            return res;
        }

        public decimal GetBalance()
        {
            string query = "SELECT TOP 1 ISNULL(balance, 0) FROM PaymentTransactionMaster ORDER BY id DESC";
            object res = _client.ExecuteScalar<object>(query, null, CommandType.Text);
            return res != null ? Convert.ToDecimal(res) : 0m;
        }
    }
}
