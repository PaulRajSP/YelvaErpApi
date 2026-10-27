using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Repository
{
    public interface ISupplierRepository
    {
        PageResult<SupplierListItem>  GetAll(GetSupplierListReq req);
        GetSupplierByIdRes? GetById(string edid);
        AddSupplierRes AddSupplier(AddSupplierReq req);
        bool Update(string edid, EditSupplierReq req);
    }

    public class SupplierRepository : ISupplierRepository
    {
        private readonly DbClient _client;
        public SupplierRepository(DbClient client) => _client = client;

        public PageResult<SupplierListItem> GetAll(GetSupplierListReq req)
        {
            int pageNumber = req.Pagination.PageNumber > 0 ? req.Pagination.PageNumber : 1;
            int pageSize   = req.Pagination.PageSize   > 0 ? req.Pagination.PageSize   : 10;
            int offset = (pageNumber - 1) * pageSize;

            string query = @"SELECT COUNT(*) OVER() AS TotalRecords, id,name,type,contactName,mobile,balance
                 FROM SupplierMaster
                 WHERE (@SupplierType IS NULL OR type = @SupplierType)
                   AND (
                       (@ContactName IS NULL AND @Name IS NULL)
                       OR (@ContactName IS NOT NULL AND contactName LIKE '%' + @ContactName + '%')
                       OR (@Name IS NOT NULL AND name LIKE '%' + @Name + '%')
                       OR (@Name IS NOT NULL AND mobile = @Name)
                   )
                 ORDER BY id DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            SqlParameter[] p = new SqlParameter[5];
            p[0] = new SqlParameter("@ContactName",  string.IsNullOrWhiteSpace(req.ContactName) ? DBNull.Value : req.ContactName);
            p[1] = new SqlParameter("@SupplierType", req.SupplierType.HasValue ? req.SupplierType.Value.ToString() : DBNull.Value);
            p[2] = new SqlParameter("@Name",         string.IsNullOrWhiteSpace(req.Name) ? DBNull.Value : req.Name);
            p[3] = new SqlParameter("@Offset",       offset);
            p[4] = new SqlParameter("@PageSize",     pageSize);

            DataSet ds = _client.ExecuteAdapter(query, p, CommandType.Text);

            PageResult<SupplierListItem> res = new PageResult<SupplierListItem>
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
                    res.Items.Add(new SupplierListItem
                    {
                        Edid        = EncryptionHelper.Encrypt(row.Field<int>("id").ToString()),
                        Name        = row.Field<string>("name")!,
                        Type        = row.Field<string?>("type"),
                        ContactName = row.Field<string?>("contactName"),
                        Mobile      = row.Field<string?>("mobile"),
                        Balance     = row.Field<decimal>("balance")
                    });
                }
            }

            return res;
        }

        public GetSupplierByIdRes? GetById(string edid)
        {
            SqlParameter[] p = { new SqlParameter("@Id", EncryptionHelper.Decrypt(edid)) };

            string    query = "SELECT id,name,type,contactName,mobile,balance FROM SupplierMaster WHERE id=@Id";
            DataTable dt    = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            return new GetSupplierByIdRes
            {
                Edid        = EncryptionHelper.Encrypt(row.Field<int>("id").ToString()),
                Name        = row.Field<string>("name")!,
                Type        = row.Field<string?>("type"),
                ContactName = row.Field<string?>("contactName"),
                Mobile      = row.Field<string?>("mobile"),
                Balance     = row.Field<decimal>("balance")
            };
        }

        public AddSupplierRes AddSupplier(AddSupplierReq req)
        {
            SqlParameter[] p = new SqlParameter[5];
            p[0] = new SqlParameter("@Name",        req.Name);
            p[1] = new SqlParameter("@Type",        Utilities.DbValue(req.Type));
            p[2] = new SqlParameter("@ContactName", Utilities.DbValue(req.ContactName));
            p[3] = new SqlParameter("@Mobile",      Utilities.DbValue(req.Mobile));
            p[4] = new SqlParameter("@Balance",     req.Balance);

            string query = "INSERT INTO SupplierMaster (name,type,contactName,mobile,balance) OUTPUT INSERTED.Id VALUES (@Name,@Type,@ContactName,@Mobile,@Balance)";
            AddSupplierRes res = new AddSupplierRes();
            res.Edid = EncryptionHelper.Encrypt(_client.ExecuteScalar<int>(query, p, CommandType.Text).ToString());

            return res;
        }

        public bool Update(string edid, EditSupplierReq req)
        {
            SqlParameter[] p = new SqlParameter[6];
            p[0] = new SqlParameter("@Id",          EncryptionHelper.Decrypt(edid));
            p[1] = new SqlParameter("@Name",        req.Name);
            p[2] = new SqlParameter("@Type",        Utilities.DbValue(req.Type));
            p[3] = new SqlParameter("@ContactName", Utilities.DbValue(req.ContactName));
            p[4] = new SqlParameter("@Mobile",      Utilities.DbValue(req.Mobile));
            p[5] = new SqlParameter("@Balance",     req.Balance);

            return _client.ExecuteNonQuery(
                "UPDATE SupplierMaster SET name=@Name,type=@Type,contactName=@ContactName,mobile=@Mobile,balance=@Balance WHERE id=@Id",
                p, CommandType.Text) > 0;
        }
    }
}
