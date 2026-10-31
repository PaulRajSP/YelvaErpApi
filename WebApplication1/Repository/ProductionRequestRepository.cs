using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Net.NetworkInformation;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Repository
{
    public interface IProductionRequestRepository
    {
        PageResult<ProductionRequestListItem> GetAll(GetProductionRequestListReq req);
        GetProductionRequestByIdRes? GetById(string edid);
        CreateProductionRequestRes CreateProductionRequest(CreateProductionRequestReq req);
        bool UpdateProductionRequestStatus(UpdateProductionRequestStatusReq req);
        GetRecipeRawMaterialsRes GetRecipeRawMaterials(string productEdid);
    }

    public class ProductionRequestRepository : IProductionRequestRepository
    {
        private readonly DbClient _client;
        public ProductionRequestRepository(DbClient client) => _client = client;

        public PageResult<ProductionRequestListItem> GetAll(GetProductionRequestListReq req)
        {
            int pageNumber = req.Pagination.PageNumber > 0 ? req.Pagination.PageNumber : 1;
            int pageSize = req.Pagination.PageSize > 0 ? req.Pagination.PageSize : 10;
            int offset = (pageNumber - 1) * pageSize;

            string query = @"SELECT COUNT(*) OVER() AS TotalRecords,
            pr.id, pr.requestNumber, pr.productId, p.name AS productName, p.sku AS productSku,
            pr.requestedQty, pr.completedQty, pr.status, pr.priority, pr.reason, pr.requestedBy,
            ISNULL((SELECT SUM(ri.qty * ISNULL(pc.averageRate, 0)) FROM RecipeItem ri
                OUTER APPLY (SELECT SUM(pi.rate * pi.quantity) / NULLIF(SUM(pi.quantity), 0) AS averageRate
                FROM PurchaseItem pi WHERE pi.materialId = ri.materialId) pc
                WHERE ri.productId = pr.productId AND (ri.isActive = 1 OR ri.isActive IS NULL)), 0) AS productionUnitCost,
            pr.createdAt
     FROM ProductionRequest pr
     LEFT JOIN ProductMaster p ON pr.productId = p.id
     WHERE (@Status IS NULL OR UPPER(pr.status) = UPPER(@Status))
       AND (@Priority IS NULL OR UPPER(pr.priority) = UPPER(@Priority))
       AND (@FromDate IS NULL OR CAST(pr.createdAt AS DATE) >= CAST(@FromDate AS DATE))
       AND (@ToDate IS NULL OR CAST(pr.createdAt AS DATE) <= CAST(@ToDate AS DATE))
       AND (@SearchKeyword IS NULL OR pr.requestNumber LIKE '%' + @SearchKeyword + '%' OR p.name LIKE '%' + @SearchKeyword + '%' OR pr.requestedBy LIKE '%' + @SearchKeyword + '%')
     ORDER BY pr.createdAt DESC
     OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            SqlParameter[] p = new SqlParameter[7];
            p[0] = new SqlParameter("@Offset", offset);
            p[1] = new SqlParameter("@PageSize", pageSize);
            p[2] = new SqlParameter("@Status", Utilities.DbValue(req.Status.ToString()));
            p[3] = new SqlParameter("@Priority", Utilities.DbValue(req.Priority.ToString()));
            p[4] = new SqlParameter("@FromDate", Utilities.DbValue(req.FromDate));
            p[5] = new SqlParameter("@ToDate", Utilities.DbValue(req.ToDate));
            p[6] = new SqlParameter("@SearchKeyword", Utilities.DbValue(req.SearchKeyword));

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            int totalRecords = 0;
            PageResult<ProductionRequestListItem> res = new PageResult<ProductionRequestListItem>
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            foreach (DataRow row in dt.Rows)
            {
                if (totalRecords == 0 && row.Table.Columns.Contains("TotalRecords") && !row.IsNull("TotalRecords"))
                {
                    totalRecords = row.Field<int>("TotalRecords");
                }

                ProductionRequestListItem item = new ProductionRequestListItem();
                item.Edid = EncryptionHelper.Encrypt(row.Field<int>("id").ToString());
                item.RequestNumber = row.Field<string>("requestNumber") ?? string.Empty;
                item.ProductEdid = EncryptionHelper.Encrypt(row.Field<int>("productId").ToString());
                item.ProductName = row.Field<string>("productName");
                item.ProductSku = row.Field<string>("productSku");
                item.RequestedQty = row.Field<decimal>("requestedQty");
                item.CompletedQty = row.Field<decimal?>("completedQty") ?? 0;
                item.Status = Enum.Parse<ProductionStatus>(row.Field<string>("status"), true);
                item.Priority = Enum.Parse<ProductionPriority>(row.Field<string>("priority"), true);
                item.Reason = row.Field<string>("reason");
                item.RequestedBy = row.Field<string>("requestedBy");
                item.ProductionUnitCost = row.Field<decimal>("productionUnitCost");
                item.CreatedAt = row.Field<DateTime>("createdAt");

                res.Items.Add(item);
            }

            res.TotalCount = totalRecords > 0 ? totalRecords : res.Items.Count;
            return res;
        }

        public GetProductionRequestByIdRes? GetById(string edid)
        {
            string id = EncryptionHelper.Decrypt(edid);
            if (string.IsNullOrWhiteSpace(id) || !int.TryParse(id, out int intId)) return null;

            SqlParameter[] p = { new SqlParameter("@Id", intId) };

            string query = @"SELECT pr.id, pr.requestNumber, pr.productId, p.name AS productName, p.sku AS productSku,
                            ISNULL((SELECT SUM(pb.remainingQty) FROM ProductBatchMaster pb WHERE pb.productId = p.id AND pb.isActive = 1), 0) AS productStock,
                            pr.requestedQty, pr.completedQty, pr.status, pr.priority,
                            pr.reason, pr.requestedBy, ISNULL(productionCost.productionUnitCost, 0) AS productionUnitCost,
                            pr.createdAt, productionBatch.packedDate, productionBatch.expiryDate
                     FROM ProductionRequest pr
                     LEFT JOIN ProductMaster p ON pr.productId = p.id
                     OUTER APPLY (
                         SELECT TOP 1 pb.packedDate, pb.expiryDate
                         FROM ProductBatchMaster pb
                         WHERE pb.productId = pr.productId
                           AND pb.remarks LIKE 'Production Request #' + pr.requestNumber + ' produced %'
                         ORDER BY pb.createdAt DESC, pb.id DESC
                     ) productionBatch
                     OUTER APPLY (
                         SELECT SUM(ri.qty * ISNULL(pc.averageRate, 0)) AS productionUnitCost
                         FROM RecipeItem ri
                         LEFT JOIN (
                             SELECT pi.materialId,
                                    SUM(pi.rate * pi.quantity) / NULLIF(SUM(pi.quantity), 0) AS averageRate
                             FROM PurchaseItem pi
                             GROUP BY pi.materialId
                         ) pc ON pc.materialId = ri.materialId
                         WHERE ri.productId = pr.productId
                           AND (ri.isActive = 1 OR ri.isActive IS NULL)
                     ) productionCost
                     WHERE pr.id = @Id";

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];
            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            int productId = row.IsNull("productId") ? 0 : Convert.ToInt32(row["productId"]);
            decimal requestedQty = row.IsNull("requestedQty") ? 0m : Convert.ToDecimal(row["requestedQty"]);

            GetProductionRequestByIdRes res = new GetProductionRequestByIdRes();
            res.Edid = EncryptionHelper.Encrypt(row.Field<int>("id").ToString());
            res.RequestNumber = row.Field<string>("requestNumber")!;
            res.ProductEdid = EncryptionHelper.Encrypt(productId.ToString());
            res.ProductName = row.Field<string>("productName");
            res.ProductSku = row.Field<string>("productSku");
            res.ProductStock = row.Field<decimal>("productStock");
            res.RequestedQty = requestedQty;
            res.CompletedQty = row.Field<decimal?>("completedQty") ?? 0;
            res.Status = Enum.Parse<ProductionStatus>(row.Field<string>("status")!, true);
            res.Priority = Enum.Parse<ProductionPriority>(row.Field<string>("priority")!, true);
            res.Reason = row.Field<string>("reason");
            res.RequestedBy = row.Field<string>("requestedBy");
            res.ProductionUnitCost = row.Field<decimal?>("productionUnitCost");
            res.CreatedAt = row.Field<DateTime>("createdAt");
            res.PackedDate = row.Field<DateTime?>("packedDate");
            res.ExpiryDate = row.Field<DateTime?>("expiryDate");

            // Query materials linked to this request
            SqlParameter[] matP = new SqlParameter[2];
            matP[0] = new SqlParameter("@Id", intId);
            matP[1] = new SqlParameter("@ProductId", productId);

            string matQuery = @"SELECT prm.id, prm.materialId, prm.materialBatchId, mb.batchNumber,
                               rm.name AS materialName, rc.name AS category,
                               u.symbol AS unitSymbol,
                               COALESCE(mb.qtyRemaining, totalStock.qty, 0) AS availableStock,
                               COALESCE(ri.qty, 1) AS recipeQty,
                               COALESCE(prm.recipeItemId, ri.id) AS recipeItemId
                        FROM ProductionRequestMaterial prm
                        INNER JOIN RawMaterialMaster rm ON prm.materialId = rm.id
                        LEFT JOIN RawMaterialCategoryMaster rc ON rm.categoryId = rc.id
                        LEFT JOIN UnitMaster u ON rm.unitId = u.id
                        LEFT JOIN RecipeItem ri ON ri.id = prm.recipeItemId
                        LEFT JOIN MaterialBatchMaster mb ON prm.materialBatchId = mb.id
                        OUTER APPLY (
                            SELECT SUM(m.qtyRemaining) AS qty
                            FROM MaterialBatchMaster m
                            WHERE m.materialId = rm.id
                              AND m.isActive = 1
                        ) totalStock
                        WHERE prm.productionRequestId = @Id";

            DataTable matDt = _client.ExecuteAdapter(matQuery, matP, CommandType.Text).Tables[0];

            foreach (DataRow matRow in matDt.Rows)
            {
                decimal recipeQty = matRow.IsNull("recipeQty") ? 1m : matRow.Field<decimal>("recipeQty");
                decimal requiredQty = recipeQty * requestedQty;
                decimal availableStock = matRow.IsNull("availableStock") ? 0m : matRow.Field<decimal>("availableStock");
                int? batchId = matRow.IsNull("materialBatchId") ? null : matRow.Field<int>("materialBatchId");
                int? recId = matRow.IsNull("recipeItemId") ? null : matRow.Field<int>("recipeItemId");

                ProductionRequestMaterialItem item = new ProductionRequestMaterialItem();
                item.Edid = EncryptionHelper.Encrypt(matRow["id"].ToString()!);
                item.MaterialEdid = EncryptionHelper.Encrypt(matRow["materialId"].ToString()!);
                item.MaterialBatchEdid = batchId.HasValue ? EncryptionHelper.Encrypt(batchId.Value.ToString()) : null;
                item.RecipeItemEdid = recId.HasValue ? EncryptionHelper.Encrypt(recId.Value.ToString()) : null;
                item.BatchNumber = matRow.Field<string>("batchNumber");
                item.MaterialName = matRow.Field<string>("materialName")!;
                item.Category = matRow.Field<string>("category");
                item.UnitSymbol = matRow.Field<string>("unitSymbol");
                item.RecipeQty = recipeQty;
                item.RequiredQty = requiredQty;
                item.AvailableStock = availableStock;
                item.IsSufficient = availableStock >= requiredQty;

                res.Materials.Add(item);
            }

            // Fallback: If no material records were found in ProductionRequestMaterial, load from RecipeItem
            if (res.Materials.Count == 0 && productId > 0)
            {
                SqlParameter[] recP = { new SqlParameter("@ProductId", productId) };

                string recQuery = @"SELECT ri.id AS recipeItemId, ri.materialId, NULL AS materialBatchId, NULL AS batchNumber,
                                   rm.name AS materialName, rc.name AS category,
                                   u.symbol AS unitSymbol,
                                   COALESCE(totalStock.qty, 0) AS availableStock,
                                   ri.qty AS recipeQty
                            FROM RecipeItem ri
                            INNER JOIN RawMaterialMaster rm ON ri.materialId = rm.id
                            LEFT JOIN RawMaterialCategoryMaster rc ON rm.categoryId = rc.id
                            LEFT JOIN UnitMaster u ON rm.unitId = u.id
                            OUTER APPLY (
                                SELECT SUM(m.qtyRemaining) AS qty
                                FROM MaterialBatchMaster m
                                WHERE m.materialId = rm.id
                                  AND m.isActive = 1
                            ) totalStock
                            WHERE ri.productId = @ProductId
                              AND (ri.isActive = 1 OR ri.isActive IS NULL)";

                DataTable recDt = _client.ExecuteAdapter(recQuery, recP, CommandType.Text).Tables[0];

                foreach (DataRow r in recDt.Rows)
                {
                    decimal recipeQty = r.IsNull("recipeQty") ? 1m : Convert.ToDecimal(r["recipeQty"]);
                    decimal requiredQty = recipeQty * requestedQty;
                    decimal availableStock = r.IsNull("availableStock") ? 0m : Convert.ToDecimal(r["availableStock"]);
                    int? recId = r.IsNull("recipeItemId") ? null : Convert.ToInt32(r["recipeItemId"]);

                    res.Materials.Add(new ProductionRequestMaterialItem
                    {
                        Edid = EncryptionHelper.Encrypt(r["recipeItemId"].ToString()!),
                        MaterialEdid = EncryptionHelper.Encrypt(r["materialId"].ToString()!),
                        MaterialBatchEdid = null,
                        RecipeItemEdid = recId.HasValue ? EncryptionHelper.Encrypt(recId.Value.ToString()) : null,
                        BatchNumber = null,
                        MaterialName = r.IsNull("materialName") ? string.Empty : r["materialName"].ToString()!,
                        Category = r.IsNull("category") ? null : r["category"].ToString(),
                        UnitSymbol = r.IsNull("unitSymbol") ? null : r["unitSymbol"].ToString(),
                        RecipeQty = recipeQty,
                        RequiredQty = requiredQty,
                        AvailableStock = availableStock,
                        IsSufficient = availableStock >= requiredQty
                    });
                }
            }

            return res;
        }

        public CreateProductionRequestRes CreateProductionRequest(CreateProductionRequestReq req)
        {
            string requestNumber = Utilities.GeneratePublicId("PRD", 5);
            DateTime now = DateTime.UtcNow;

            SqlParameter[] p = new SqlParameter[8];
            p[0] = new SqlParameter("@RequestNumber",requestNumber);
            p[1] = new SqlParameter("@ProductId", EncryptionHelper.Decrypt(req.ProductEdid));
            p[2] = new SqlParameter("@RequestedQty",req.RequestedQty);
            p[3] = new SqlParameter("@Status", req.Status.ToString());
            p[4] = new SqlParameter("@Priority", Utilities.DbValue(req.Priority.ToString()));
            p[5] = new SqlParameter("@Reason", Utilities.DbValue(req.Reason));
            p[6] = new SqlParameter("@RequestedBy", Utilities.DbValue(req.RequestedBy));
            p[7] = new SqlParameter("@CreatedAt", now);

            string insertQuery = @"INSERT INTO ProductionRequest(requestNumber, productId, requestedQty, completedQty, status, priority, reason, requestedBy, createdAt)
                                   OUTPUT INSERTED.id VALUES (@RequestNumber, @ProductId, @RequestedQty, 0, @Status, @Priority, @Reason, @RequestedBy, @CreatedAt)";

            int requestId = _client.ExecuteScalar<int>(insertQuery, p, CommandType.Text);

            // Populate materials
            if (req.Materials != null && req.Materials.Count > 0)
            {
                foreach (CreateProductionRequestMaterialLine line in req.Materials)
                {
                    SqlParameter[] mp = new SqlParameter[5];
                    mp[0] = new SqlParameter("@RequestId", requestId);
                    mp[1] = new SqlParameter("@MaterialId", EncryptionHelper.Decrypt(line.MaterialEdid));
                    mp[2] = new SqlParameter("@MaterialBatchId", Utilities.DbValue(EncryptionHelper.Decrypt<int?>(line.MaterialBatchEdid)));
                    mp[3] = new SqlParameter("@RecipeItemId", Utilities.DbValue(EncryptionHelper.Decrypt<int?>(line.RecipeItemEdid)));
                    mp[4] = new SqlParameter("@CreatedAt", now);

                    string insertMatQuery = @"INSERT INTO ProductionRequestMaterial (productionRequestId, materialId, materialBatchId, recipeItemId, createdAt)
                                              VALUES (@RequestId, @MaterialId, @MaterialBatchId, @RecipeItemId, @CreatedAt)";

                    _client.ExecuteNonQuery(insertMatQuery, mp, CommandType.Text);
                }
            }

            CreateProductionRequestRes resp = new CreateProductionRequestRes();
            resp.Edid = EncryptionHelper.Encrypt(requestId.ToString());
            resp.RequestNumber = requestNumber;

            return resp;
        }

        public bool UpdateProductionRequestStatus(UpdateProductionRequestStatusReq req)
        {
            SqlParameter[] p = new SqlParameter[3];
            p[0] = new SqlParameter("@Id", EncryptionHelper.Decrypt(req.Edid));
            p[1] = new SqlParameter("@Status", req.Status.ToString());
            p[2] = new SqlParameter("@CompletedQty", req.CompletedQty);

            string query = @"UPDATE ProductionRequest SET status = @Status, completedQty = ISNULL(@CompletedQty, completedQty) WHERE id = @Id";

            int rowsAffected = _client.ExecuteNonQuery(query, p, CommandType.Text);

            return rowsAffected > 0;
        }



        public GetRecipeRawMaterialsRes GetRecipeRawMaterials(string productEdid)
        {
            string productId = EncryptionHelper.Decrypt(productEdid);
            if (string.IsNullOrWhiteSpace(productId))
                return new GetRecipeRawMaterialsRes();

            SqlParameter[] p = new SqlParameter[1];
            p[0] = new SqlParameter("@ProductId", productId);

            string query = @"SELECT ri.id AS recipeItemId, ri.materialId, ri.qty,
                rm.name AS materialName, rc.name AS category,
                COALESCE(u.symbol, u2.symbol, '') AS allowedUnitSymbol,
                mb.id AS batchId, mb.batchNumber, mb.qtyRemaining,
                CASE
                    WHEN mb.id IS NULL THEN 0
                    ELSE mb.qtyRemaining - ISNULL(reserved.reservedQty, 0)
                END AS availableQty
            FROM RecipeItem ri
            INNER JOIN RawMaterialMaster rm ON ri.materialId = rm.id
            LEFT JOIN RawMaterialCategoryMaster rc ON rm.categoryId = rc.id
            LEFT JOIN UnitMaster u ON u.id = rm.unitId
            LEFT JOIN UnitMaster u2 ON u2.id = rc.allowedUnitId
            LEFT JOIN MaterialBatchMaster mb ON mb.materialId = ri.materialId
                 AND (mb.isActive = 1 OR mb.isActive IS NULL)
                 AND mb.qtyRemaining > 0
            OUTER APPLY (
                SELECT SUM(
                    (pr.requestedQty - ISNULL(pr.completedQty, 0)) * ISNULL(ri2.qty, 1)
                ) AS reservedQty
                FROM ProductionRequestMaterial prm
                INNER JOIN ProductionRequest pr ON pr.id = prm.productionRequestId
                LEFT JOIN RecipeItem ri2 ON ri2.id = prm.recipeItemId
                WHERE prm.materialBatchId = mb.id
                  AND UPPER(pr.status) NOT IN ('COMPLETED')
            ) reserved
            WHERE ri.productId = @ProductId AND (ri.isActive = 1 OR ri.isActive IS NULL) ORDER BY ri.id ASC, mb.id ASC";

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            GetRecipeRawMaterialsRes response = new GetRecipeRawMaterialsRes();
            Dictionary<int, ProductionRequestRecipeRawMaterialItem> materialMap = new Dictionary<int, ProductionRequestRecipeRawMaterialItem>();

            foreach (DataRow row in dt.Rows)
            {
                int recipeItemId = row.Field<int>("recipeItemId");
                int materialId = row.Field<int>("materialId");

                if (!materialMap.ContainsKey(recipeItemId))
                {
                    ProductionRequestRecipeRawMaterialItem item = new ProductionRequestRecipeRawMaterialItem();
                    item.RecipeItemEdid = EncryptionHelper.Encrypt(recipeItemId.ToString());
                    item.MaterialEdid = EncryptionHelper.Encrypt(materialId.ToString());
                    item.MaterialName = row.Field<string>("materialName");
                    item.Category = row.Field<string>("category");
                    item.AllowedUnitSymbol = row.Field<string>("allowedUnitSymbol");
                    item.Qty = row.Field<decimal?>("qty");
                    item.RecipeBatches = new List<ProductionRequestRecipeBatchItem>();

                    materialMap.Add(recipeItemId, item);
                    response.RawMaterials.Add(item);
                }

                if (!row.IsNull("batchId"))
                {
                    decimal? availableQty = row.Field<decimal?>("availableQty");

                    if (availableQty.HasValue && availableQty.Value > 0)
                    {
                        int batchId = row.Field<int>("batchId");
                        ProductionRequestRecipeRawMaterialItem item = materialMap[recipeItemId];
                        bool isDefault = item.RecipeBatches.Count == 0;

                        ProductionRequestRecipeBatchItem batch = new ProductionRequestRecipeBatchItem();
                        batch.Edid = EncryptionHelper.Encrypt(batchId.ToString());
                        batch.MaterialEdid = EncryptionHelper.Encrypt(materialId.ToString());
                        batch.BatchNumber = row.Field<string>("batchNumber");
                        batch.QtyAvailable = availableQty;
                        batch.MaxProductionPossibleQty = item.Qty.HasValue && item.Qty.Value > 0
                                                         ? Math.Floor(availableQty.Value / item.Qty.Value) : 0;
                        batch.IsDefault = isDefault;

                        item.RecipeBatches.Add(batch);
                    }
                }
            }

            return response;
        }


    }
}
