using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace HoneyErpAPI.Repository
{
    public interface IBatchRepository
    {
        GetBatchListRes GetBatches(string materialEdid, string? batchNumber);
        GetBatchByIdRes? GetBatchById(string edid);
        CreateBatchRes CreateBatch(CreateBatchReq req);
        bool UpdateBatchQty(string edid, decimal qtyRemaining);
    }

    public class BatchRepository : IBatchRepository
    {
        private readonly DbClient _client;
        public BatchRepository(DbClient client) => _client = client;

        public GetBatchListRes GetBatches(string materialEdid, string? batchNumber)
        {
            SqlParameter[] p = new SqlParameter[2];
            p[0] = new SqlParameter("@batchNumber", Utilities.DbValue(batchNumber));
            p[1] = new SqlParameter("@MaterialId", EncryptionHelper.Decrypt(materialEdid));

            string query = @"SELECT id, materialId, batchNumber, qtyPurchased, qtyRemaining
                             FROM MaterialBatchMaster WHERE materialId = @MaterialId AND (@batchNumber IS NULL OR batchNumber = @batchNumber)";


            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];
            GetBatchListRes res = new GetBatchListRes();

            foreach (DataRow row in dt.Rows)
            {   
                res.Items.Add(new BatchListItem
                {
                    Edid         = EncryptionHelper.Encrypt(row.Field<int>("id").ToString()),
                    MaterialEdid = EncryptionHelper.Encrypt(row.Field<int>("materialId").ToString()),
                    BatchNumber  = row.Field<string>("batchNumber")!,
                    QtyPurchased = row.Field<decimal>("qtyPurchased"),
                    QtyRemaining = row.Field<decimal>("qtyRemaining")
                });
            }
            return res;
        }

        public GetBatchByIdRes? GetBatchById(string edid)
        {
            SqlParameter[] p = { new SqlParameter("@Id", EncryptionHelper.Decrypt(edid)) };
            string query = "SELECT id,materialId,batchNumber,qtyPurchased,qtyRemaining FROM MaterialBatchMaster WHERE id=@Id";
            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            return new GetBatchByIdRes
            {
                Edid         = EncryptionHelper.Encrypt(row.Field<int>("id").ToString()),
                MaterialEdid = EncryptionHelper.Encrypt(row.Field<int>("materialId").ToString()),
                BatchNumber  = row.Field<string>("batchNumber")!,
                QtyPurchased = row.Field<decimal>("qtyPurchased"),
                QtyRemaining = row.Field<decimal>("qtyRemaining")
            };
        }

        public CreateBatchRes CreateBatch(CreateBatchReq req)
        {
            SqlParameter[] p = new SqlParameter[4];
            p[0] = new SqlParameter("@MaterialId",  EncryptionHelper.Decrypt(req.MaterialEdid));
            p[1] = new SqlParameter("@BatchNumber", req.BatchNumber);
            p[2] = new SqlParameter("@QtyPurchased",req.QtyPurchased);
            p[3] = new SqlParameter("@QtyRemaining",req.QtyRemaining);

            string query = "INSERT INTO MaterialBatchMaster (materialId,batchNumber,qtyPurchased,qtyRemaining) OUTPUT INSERTED.Id VALUES (@MaterialId,@BatchNumber,@QtyPurchased,@QtyRemaining)";

            CreateBatchRes res = new CreateBatchRes();
            res.BatchEdid = EncryptionHelper.Encrypt(_client.ExecuteScalar<int>(query, p, CommandType.Text).ToString());
            res.BatchNumber = req.BatchNumber;
            
            return res;
        }

        public bool UpdateBatchQty(string edid, decimal qtyRemaining)
        {
            SqlParameter[] p = new SqlParameter[2];
            p[0] = new SqlParameter("@Id",           EncryptionHelper.Decrypt(edid));
            p[1] = new SqlParameter("@QtyRemaining", qtyRemaining);
            return _client.ExecuteNonQuery("UPDATE MaterialBatchMaster SET qtyRemaining=@QtyRemaining WHERE id=@Id", p, CommandType.Text) > 0;
        }
    }
}
