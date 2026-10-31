using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace HoneyErpAPI.Repository
{
    public interface IBatchRepository
    {
        GetBatchListRes GetBatches(string materialEdid, string? batchNumber, decimal? unitCost, string? supplierEdid);
        GetBatchByIdRes? GetBatchById(string edid);
        CreateBatchRes CreateBatch(CreateBatchReq req);
    }

    public class BatchRepository : IBatchRepository
    {
        private readonly DbClient _client;

        public BatchRepository(DbClient client) => _client = client;

        public GetBatchListRes GetBatches(string materialEdid, string? batchNumber, decimal? unitCost, string? supplierEdid)
        {
            SqlParameter[] p = new SqlParameter[4];
            p[0] = new SqlParameter("@batchNumber", Utilities.DbValue(batchNumber));
            p[1] = new SqlParameter("@MaterialId", EncryptionHelper.Decrypt(materialEdid));
            p[2] = new SqlParameter("@UnitCost", Utilities.DbValue(unitCost));
            p[3] = new SqlParameter("@SupplierId", Utilities.DbValue(EncryptionHelper.Decrypt(supplierEdid)));

            string query = @"SELECT id, materialId, supplierId, batchNumber, qtyPurchased, qtyRemaining, ISNULL(unitCost, 0) AS unitCost
                             FROM MaterialBatchMaster
                             WHERE materialId = @MaterialId
                               AND (isActive = 1 OR isActive IS NULL)
                               AND (@batchNumber IS NULL OR batchNumber LIKE '%' + @batchNumber + '%')
                               AND (@UnitCost IS NULL OR unitCost <= @UnitCost AND @UnitCost < unitCost + 0.10)
                               AND (@SupplierId IS NULL OR supplierId = @SupplierId)";


            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];
            GetBatchListRes res = new GetBatchListRes();

            foreach (DataRow row in dt.Rows)
            {   
                res.Items.Add(new BatchListItem
                {
                    Edid         = EncryptionHelper.Encrypt(row.Field<int>("id").ToString()),
                    MaterialEdid = EncryptionHelper.Encrypt(row.Field<int>("materialId").ToString()),
                    SupplierEdid = EncryptionHelper.Encrypt(row.Field<int>("supplierId").ToString()),
                    BatchNumber  = row.Field<string>("batchNumber")!,
                    QtyPurchased = row.Field<decimal>("qtyPurchased"),
                    QtyRemaining = row.Field<decimal>("qtyRemaining"),
                    UnitCost     = row.Field<decimal>("unitCost")
                });
            }
            return res;
        }

        public GetBatchByIdRes? GetBatchById(string edid)
        {
            SqlParameter[] p = { new SqlParameter("@Id", EncryptionHelper.Decrypt(edid)) };
            string query = "SELECT id,materialId,supplierId,batchNumber,qtyPurchased,qtyRemaining,ISNULL(unitCost, 0) AS unitCost FROM MaterialBatchMaster WHERE id=@Id";
            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            return new GetBatchByIdRes
            {
                Edid         = EncryptionHelper.Encrypt(row.Field<int>("id").ToString()),
                MaterialEdid = EncryptionHelper.Encrypt(row.Field<int>("materialId").ToString()),
                SupplierEdid = row.IsNull("supplierId") ? null : EncryptionHelper.Encrypt(row.Field<int>("supplierId").ToString()),
                BatchNumber  = row.Field<string>("batchNumber")!,
                QtyPurchased = row.Field<decimal>("qtyPurchased"),
                QtyRemaining = row.Field<decimal>("qtyRemaining"),
                UnitCost     = row.Field<decimal>("unitCost")
            };
        }

        public CreateBatchRes CreateBatch(CreateBatchReq req)
        {
            SqlParameter[] p = new SqlParameter[6];
            p[0] = new SqlParameter("@MaterialId", EncryptionHelper.Decrypt(req.MaterialEdid));
            p[1] = new SqlParameter("@BatchNumber", req.BatchNumber);
            p[2] = new SqlParameter("@QtyPurchased", req.QtyPurchased);
            p[3] = new SqlParameter("@QtyRemaining", req.QtyRemaining);
            p[4] = new SqlParameter("@UnitCost", req.UnitCost);
            p[5] = new SqlParameter("@SupplierId", string.IsNullOrWhiteSpace(req.SupplierEdid)
                ? DBNull.Value
                : EncryptionHelper.Decrypt(req.SupplierEdid));

            string query = "INSERT INTO MaterialBatchMaster (materialId,supplierId,batchNumber,qtyPurchased,qtyRemaining,unitCost) OUTPUT INSERTED.Id VALUES (@MaterialId,@SupplierId,@BatchNumber,@QtyPurchased,@QtyRemaining,@UnitCost)";

            CreateBatchRes res = new CreateBatchRes();
            res.BatchEdid = EncryptionHelper.Encrypt(_client.ExecuteScalar<int>(query, p, CommandType.Text).ToString());
            res.BatchNumber = req.BatchNumber;

            return res;
        }

    }
}
