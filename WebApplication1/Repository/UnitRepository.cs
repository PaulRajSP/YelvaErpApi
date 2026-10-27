using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace HoneyErpAPI.Repository
{
    public interface IUnitRepository
    {
        GetUnitListRes  GetAll();
        string Create(AddUnitReq req);
    }

    public class UnitRepository : IUnitRepository
    {
        private readonly DbClient _client;
        public UnitRepository(DbClient client) => _client = client;

        public GetUnitListRes GetAll()
        {
            string query = @"SELECT id, name, symbol FROM UnitMaster ORDER BY name";

            DataTable dt = _client.ExecuteAdapter(query, null, CommandType.Text).Tables[0];

            GetUnitListRes res = new GetUnitListRes();

            foreach (DataRow row in dt.Rows)
            {
                res.Items.Add(new UnitListItem
                {
                    Edid   = EncryptionHelper.Encrypt(row.Field<int>("id").ToString()),
                    Name   = row.Field<string>("name")!,
                    Symbol = row.Field<string>("symbol")!
                });
            }

            return res;
        }

        public string Create(AddUnitReq req)
        {
            SqlParameter[] p = new SqlParameter[2];
            p[0] = new SqlParameter("@Name",   req.Name);
            p[1] = new SqlParameter("@Symbol", req.Symbol);

            string query = "INSERT INTO UnitMaster (name, symbol) OUTPUT INSERTED.Id VALUES (@Name, @Symbol)";
            int newId = _client.ExecuteScalar<int>(query, p, CommandType.Text);
            return newId.ToString();
        }

    }
}
