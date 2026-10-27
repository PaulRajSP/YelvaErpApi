using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Repository
{
    public interface ITransactionRepository
    {
        PageResult<TransactionListItem>   GetAll(GetTransactionListReq req);
        PageResult<TransactionListItem>   GetByCategory(string category, GetTransactionListReq req);
        GetTransactionByIdRes?  GetById(string edid);
        string                  Create(CreateTransactionReq req);
    }

    public class TransactionRepository : ITransactionRepository
    {
        private readonly DbClient _client;
        public TransactionRepository(DbClient client) => _client = client;

        public PageResult<TransactionListItem> GetAll(GetTransactionListReq req)
        {
            int pageNumber = req.Pagination.PageNumber > 0 ? req.Pagination.PageNumber : 1;
            int pageSize   = req.Pagination.PageSize   > 0 ? req.Pagination.PageSize   : 10;

            int totalCount = _client.ExecuteScalar<int>("SELECT COUNT(*) FROM PaymentTransactionMaster", null, CommandType.Text);

            string query = @"SELECT id, createdAt AS date, status AS type, category, amount, remark AS description, isActive
                             FROM PaymentTransactionMaster
                             ORDER BY id DESC
                             OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            SqlParameter[] p = new SqlParameter[2];
            p[0] = new SqlParameter("@Offset",   (pageNumber - 1) * pageSize);
            p[1] = new SqlParameter("@PageSize", pageSize);

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            PageResult<TransactionListItem> res = new PageResult<TransactionListItem>
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize   = pageSize
            };

            foreach (DataRow row in dt.Rows)
            {
                res.Items.Add(MapRow(row));
            }

            return res;
        }

        public PageResult<TransactionListItem> GetByCategory(string category, GetTransactionListReq req)
        {
            int pageNumber = req.Pagination.PageNumber > 0 ? req.Pagination.PageNumber : 1;
            int pageSize   = req.Pagination.PageSize   > 0 ? req.Pagination.PageSize   : 10;

            SqlParameter[] countP = { new SqlParameter("@Category", category) };
            int totalCount = _client.ExecuteScalar<int>("SELECT COUNT(*) FROM PaymentTransactionMaster WHERE UPPER(category)=UPPER(@Category)", countP, CommandType.Text);

            SqlParameter[] p = new SqlParameter[3];
            p[0] = new SqlParameter("@Category", category);
            p[1] = new SqlParameter("@Offset",   (pageNumber - 1) * pageSize);
            p[2] = new SqlParameter("@PageSize", pageSize);

            string query = @"SELECT id, createdAt AS date, status AS type, category, amount, remark AS description, isActive
                  FROM PaymentTransactionMaster
                  WHERE UPPER(category)=UPPER(@Category)
                  ORDER BY id DESC
                  OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            PageResult<TransactionListItem> res = new PageResult<TransactionListItem>
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize   = pageSize
            };

            foreach (DataRow row in dt.Rows)
            {
                res.Items.Add(MapRow(row));
            }

            return res;
        }

        public GetTransactionByIdRes? GetById(string edid)
        {
            SqlParameter[] p = { new SqlParameter("@Id", EncryptionHelper.Decrypt(edid)) };

            string query = "SELECT id, createdAt AS date, status AS type, category, amount, remark AS description, isActive FROM PaymentTransactionMaster WHERE id=@Id";
            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            return new GetTransactionByIdRes
            {
                Edid        = EncryptionHelper.Encrypt(row.Field<int>("id").ToString()),
                Date        = row.Field<DateTime>("date"),
                Type        = row["type"]?.ToString() ?? "",
                Category    = row["category"]?.ToString() ?? "",
                Amount      = Convert.ToDecimal(row["amount"]),
                Description = row["description"]?.ToString(),
                IsActive    = !row.Table.Columns.Contains("isActive") || row["isActive"] == DBNull.Value || Convert.ToBoolean(row["isActive"])
            };
        }

        public string Create(CreateTransactionReq req)
        {
            string txnNumber = Utilities.GeneratePublicId("TXN", 6);
            SqlParameter[] p = new SqlParameter[6];
            p[0] = new SqlParameter("@TransactionNumber", txnNumber);
            p[1] = new SqlParameter("@Remark",            Utilities.DbValue(req.Description));
            p[2] = new SqlParameter("@Amount",            req.Amount);
            p[3] = new SqlParameter("@Status",            req.Type ?? "Credit");
            p[4] = new SqlParameter("@Category",          req.Category ?? TransactionCategory.Adjustment.ToString());
            p[5] = new SqlParameter("@IsActive",          req.IsActive);

            string query = @"
                DECLARE @PrevBalance DECIMAL(18,2) = 0;
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
                    SET @NewBalance = @PrevBalance;

                INSERT INTO PaymentTransactionMaster (transactionNumber, remark, amount, status, category, isActive, balance, createdAt)
                OUTPUT INSERTED.Id
                VALUES (@TransactionNumber, @Remark, @Amount, @Status, @Category, @IsActive, @NewBalance, SYSUTCDATETIME());";

            return _client.ExecuteScalar<int>(query, p, CommandType.Text).ToString();
        }

        private static TransactionListItem MapRow(DataRow row) => new TransactionListItem
        {
            Edid        = EncryptionHelper.Encrypt(row["id"].ToString() ?? "0"),
            Date        = Convert.ToDateTime(row["date"]),
            Type        = row["type"]?.ToString() ?? "",
            Category    = row["category"]?.ToString() ?? "",
            Amount      = Convert.ToDecimal(row["amount"]),
            Description = row["description"]?.ToString(),
            IsActive    = !row.Table.Columns.Contains("isActive") || row["isActive"] == DBNull.Value || Convert.ToBoolean(row["isActive"])
        };
    }
}
