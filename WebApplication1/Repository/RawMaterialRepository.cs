using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Repository
{
    public interface IRawMaterialRepository
    {
        PageResult<RawMaterialListItem> GetAll(GetRawMaterialListReq req);
        GetRawMaterialByIdRes? GetById(string edid);
        AddRawMaterialRes Create(AddRawMaterialReq req);
        bool Update(string edid, EditRawMaterialReq req);
        bool UpdateStock(string edid, decimal qtyChange);
        GetRecipeRes GetRecipe(string productEdid);
        void ToggleRecipeItemStatus(string productEdid, string materialEdid, bool active);
        void SaveRecipe(string productEdid, SaveRecipeReq req);
        PageResult<CategoryListItem> GetCategories(GetCategoryListReq req);
        AddCategoryRes CreateCategory(AddCategoryReq req);
        PageResult<MaterialPurchaseHistoryItem> GetPurchaseHistory(string edid, GetMaterialPurchaseHistoryReq req);
        MaterialUsageHistoryRes GetUsageHistory(GetMaterialUsageHistoryReq req);
        bool AddMaterialBatchUsage(MaterialBatchUsage req);
        PageResult<GetMaterialBatchListItem> GetMaterialBatchList(GetMaterialBatchListReq req);
        GetPackingMaterialsRes GetPackingMaterials(string? searchKeyword = null);
    }

    public class RawMaterialRepository : IRawMaterialRepository
    {
        private readonly DbClient _client;
        public RawMaterialRepository(DbClient client) => _client = client;


        public PageResult<RawMaterialListItem> GetAll(GetRawMaterialListReq req)
        {
            int pageNumber = req.Pagenation.PageNumber > 0 ? req.Pagenation.PageNumber : 1;
            int pageSize = req.Pagenation.PageSize > 0 ? req.Pagenation.PageSize : 10;
            int offset = (pageNumber - 1) * pageSize;

            string query = @"SELECT COUNT(*) OVER() AS TotalRecords, rm.id, rm.name, rm.unitId,
    ISNULL(mb.stock, 0) AS stock, rm.minQuantity, rm.reorderLevel,
    rc.id AS categoryId, rc.name AS category, u.symbol AS allowedUnitSymbol,
    rc.allowedUnitId,
    CASE 
        WHEN ISNULL(mb.stock, 0) <= 0 THEN 2 
        WHEN ISNULL(mb.stock, 0) <= rm.reorderLevel THEN 1 
        ELSE 0 
    END AS statusId
    FROM RawMaterialMaster rm
    INNER JOIN RawMaterialCategoryMaster rc ON rm.categoryId = rc.id
    LEFT JOIN UnitMaster u ON u.id = rc.allowedUnitId
    OUTER APPLY
    (
        SELECT SUM(m.qtyRemaining) AS stock
        FROM MaterialBatchMaster m
        WHERE m.materialId = rm.id AND m.isActive = 1
    ) mb
    WHERE (@SearchKeyword IS NULL OR rm.name LIKE '%' + @SearchKeyword + '%')
      AND (@Category IS NULL OR rc.name = @Category)
      AND (@Status IS NULL OR 
           (@Status = 2 AND ISNULL(mb.stock, 0) <= 0) OR 
           (@Status = 1 AND ISNULL(mb.stock, 0) > 0 AND ISNULL(mb.stock, 0) <= rm.reorderLevel) OR 
           (@Status = 0 AND ISNULL(mb.stock, 0) > rm.reorderLevel))
    ORDER BY rm.name OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            SqlParameter[] p = new SqlParameter[5];
            p[0] = new SqlParameter("@SearchKeyword", Utilities.DbValue(req.SearchKeyword));
            p[1] = new SqlParameter("@Category", Utilities.DbValue(req.Category));
            p[2] = new SqlParameter("@Status", req.Status.HasValue ? (object)(int)req.Status.Value : DBNull.Value);
            p[3] = new SqlParameter("@Offset", offset);
            p[4] = new SqlParameter("@PageSize", pageSize);

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            PageResult<RawMaterialListItem> res = new PageResult<RawMaterialListItem>
            {
                TotalCount = dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["TotalRecords"]) : 0,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            foreach (DataRow row in dt.Rows)
            {
                StockStatus status = (StockStatus)row.Field<int>("statusId");

                res.Items.Add(new RawMaterialListItem
                {
                    Edid = EncryptionHelper.Encrypt(row.Field<int>("id").ToString()),
                    Name = row.Field<string>("name")!,
                    Category = row.Field<string>("category")!,
                    UnitEdid = EncryptionHelper.Encrypt(row.Field<int>("unitId").ToString()),
                    Stock = Convert.ToDecimal(row["stock"]),
                    MinQuantity = row.Field<decimal>("minQuantity"),
                    ReorderLevel = row.Field<decimal>("reorderLevel"),
                    AllowedUnitEdid = EncryptionHelper.Encrypt(row.Field<int>("allowedUnitId").ToString()),
                    AllowedUnitSymbol = row.Field<string?>("allowedUnitSymbol") ?? string.Empty,
                    Status = status
                });
            }

            return res;
        }

        public GetRawMaterialByIdRes? GetById(string edid)
        {
            SqlParameter[] p = { new SqlParameter("@Id", EncryptionHelper.Decrypt(edid)) };

            string query = @"SELECT rm.id, rm.name, rc.name AS category, rm.unitId,
                            rc.allowedUnitId, u.symbol AS allowedUnitSymbol,
                            ISNULL(mb.stock, 0) AS stock, rm.minQuantity, rm.reorderLevel,
                            CASE 
                                WHEN ISNULL(mb.stock, 0) <= 0 THEN 2 
                                WHEN ISNULL(mb.stock, 0) <= rm.reorderLevel THEN 1 
                                ELSE 0 
                            END AS statusId
                     FROM RawMaterialMaster rm
                     LEFT JOIN RawMaterialCategoryMaster rc ON rm.categoryId = rc.id
                     LEFT JOIN UnitMaster u ON u.id = rc.allowedUnitId
                     OUTER APPLY
                     (
                         SELECT SUM(m.qtyRemaining) AS stock
                         FROM MaterialBatchMaster m
                         WHERE m.materialId = rm.id AND m.isActive = 1
                     ) mb
                     WHERE rm.id = @Id";

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            StockStatus status = (StockStatus)row.Field<int>("statusId");

            return new GetRawMaterialByIdRes
            {
                Edid = EncryptionHelper.Encrypt(row.Field<int>("id").ToString()),
                Name = row.Field<string>("name")!,
                Category = row.Field<string>("category")!,
                UnitEdid = EncryptionHelper.Encrypt(row.Field<int>("unitId").ToString()),
                Stock = Convert.ToDecimal(row["stock"]),
                MinQuantity = row.Field<decimal>("minQuantity"),
                ReorderLevel = row.Field<decimal>("reorderLevel"),
                Status = status
            };
        }
        public AddRawMaterialRes Create(AddRawMaterialReq req)
        {
            string unitId = EncryptionHelper.Decrypt(req.UnitEdid);
            string categoryId = EncryptionHelper.Decrypt(req.CategoryEdid);

            SqlParameter[] p = new SqlParameter[5];
            p[0] = new SqlParameter("@Name", req.Name);
            p[1] = new SqlParameter("@CategoryId", categoryId);
            p[2] = new SqlParameter("@UnitId", unitId);
            p[3] = new SqlParameter("@MinQuantity", req.MinQuantity);
            p[4] = new SqlParameter("@ReorderLevel", req.ReorderLevel);

            string query = @"INSERT INTO RawMaterialMaster (name,categoryId,unitId,minQuantity,reorderLevel,isActive)
                     OUTPUT INSERTED.Id VALUES (@Name,@CategoryId,@UnitId,@MinQuantity,@ReorderLevel,1)";

            AddRawMaterialRes resp = new AddRawMaterialRes();
            resp.Edid = EncryptionHelper.Encrypt(_client.ExecuteScalar<int>(query, p, CommandType.Text).ToString());

            return resp;
        }

        public bool Update(string edid, EditRawMaterialReq req)
        {
            string id     = EncryptionHelper.Decrypt(edid);
            string unitId = EncryptionHelper.Decrypt(req.UnitEdid);

            SqlParameter[] p = new SqlParameter[6];
            p[0] = new SqlParameter("@Id",           id);
            p[1] = new SqlParameter("@Name",         req.Name);
            p[2] = new SqlParameter("@Category",     req.Category);
            p[3] = new SqlParameter("@UnitId",       unitId);
            p[4] = new SqlParameter("@MinQuantity",  req.MinQuantity);
            p[5] = new SqlParameter("@ReorderLevel", req.ReorderLevel);

            return _client.ExecuteNonQuery(
                @"UPDATE RawMaterialMaster 
                  SET name=@Name,
                      unitId=@UnitId,
                      minQuantity=@MinQuantity,
                      reorderLevel=@ReorderLevel,
                      categoryId = ISNULL((SELECT TOP 1 id FROM RawMaterialCategoryMaster WHERE name = @Category OR id = TRY_CAST(@Category AS INT)), categoryId)
                  WHERE id=@Id",
                p, CommandType.Text) > 0;
        }

        public bool UpdateStock(string edid, decimal qtyChange)
        {
            string id = EncryptionHelper.Decrypt(edid);

            SqlParameter[] checkP = { new SqlParameter("@Id", id) };
            int exists = _client.ExecuteScalar<int>("SELECT COUNT(1) FROM RawMaterialMaster WHERE id = @Id AND isActive = 1", checkP, CommandType.Text);
            if (exists == 0) return false;

            SqlParameter[] p = new SqlParameter[2];
            p[0] = new SqlParameter("@Id", id);
            p[1] = new SqlParameter("@QtyChange", qtyChange);

            string query = @"
            IF EXISTS (SELECT 1 FROM MaterialBatchMaster WHERE materialId = @Id AND isActive = 1)
            BEGIN
                UPDATE MaterialBatchMaster 
                SET qtyRemaining = ISNULL(qtyRemaining, 0) + @QtyChange,
                    qtyPurchased = ISNULL(qtyPurchased, 0) + (CASE WHEN @QtyChange > 0 THEN @QtyChange ELSE 0 END)
                WHERE id = (SELECT TOP 1 id FROM MaterialBatchMaster WHERE materialId = @Id AND isActive = 1 ORDER BY id DESC)
            END
            ELSE
            BEGIN
                INSERT INTO MaterialBatchMaster (materialId, batchNumber, qtyPurchased, qtyRemaining, isActive)
                VALUES (@Id, 'DEFAULT', CASE WHEN @QtyChange > 0 THEN @QtyChange ELSE 0 END, @QtyChange, 1)
            END";

            _client.ExecuteNonQuery(query, p, CommandType.Text);
            return true;
        }

        public GetRecipeRes GetRecipe(string productEdid)
        {
            string productId = EncryptionHelper.Decrypt(productEdid);
            SqlParameter[] p = { new SqlParameter("@ProductId", productId) };

            string query = @"SELECT ri.id AS recipeItemId, ri.materialId, ri.qty, rm.name AS materialName, rc.name AS category, u.symbol AS allowedUnitSymbol 
                     FROM RecipeItem ri
                     INNER JOIN RawMaterialMaster rm ON ri.materialId = rm.id
                     LEFT JOIN RawMaterialCategoryMaster rc ON rm.categoryId = rc.id
                     LEFT JOIN UnitMaster u ON u.id = rm.unitId
                     WHERE ri.productId = @ProductId AND (ri.isActive = 1 OR ri.isActive IS NULL)";

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            GetRecipeRes res = new GetRecipeRes();

            foreach (DataRow row in dt.Rows)
            {
                int rId = row.Field<int>("recipeItemId");
                res.Items.Add(new RecipeItemLine
                {
                    RecipeItemId = rId,
                    RecipeItemEdid = EncryptionHelper.Encrypt(rId.ToString()),
                    MaterialEdid = EncryptionHelper.Encrypt(row.Field<int>("materialId").ToString()),
                    MaterialName = row.Field<string>("materialName") ?? string.Empty,
                    Category = row.Field<string>("category") ?? string.Empty,
                    AllowedUnitSymbol = row.Field<string>("allowedUnitSymbol") ?? string.Empty,
                    Qty = row.Field<decimal>("qty")
                });
            }

            return res;
        }
        public void ToggleRecipeItemStatus(string productEdid, string materialEdid, bool active)
        {
            string productId = EncryptionHelper.Decrypt(productEdid);
            string materialId = EncryptionHelper.Decrypt(materialEdid);

            SqlParameter[] p = new SqlParameter[3];
            p[0] = new SqlParameter("@ProductId", productId);
            p[1] = new SqlParameter("@MaterialId", materialId);
            p[2] = new SqlParameter("@Active", active);

            _client.ExecuteNonQuery("UPDATE RecipeItem SET isActive=@Active WHERE productId=@ProductId AND materialId=@MaterialId", p, CommandType.Text);
        }

        public void SaveRecipe(string productEdid, SaveRecipeReq req)
        {
            string productId = EncryptionHelper.Decrypt(productEdid);

            SqlParameter[] del = { new SqlParameter("@ProductId", productId) };
            _client.ExecuteNonQuery("DELETE FROM RecipeItem WHERE productId=@ProductId", del, CommandType.Text);

            foreach (SaveRecipeItemLine item in req.Items)
            {
                string materialId = EncryptionHelper.Decrypt(item.MaterialEdid);

                SqlParameter[] p = new SqlParameter[3];
                p[0] = new SqlParameter("@ProductId",  productId);
                p[1] = new SqlParameter("@MaterialId", materialId);
                p[2] = new SqlParameter("@Qty",        item.Qty);

                _client.ExecuteNonQuery("INSERT INTO RecipeItem (productId,materialId,qty) VALUES (@ProductId,@MaterialId,@Qty)", p, CommandType.Text);
            }
        }

        public PageResult<CategoryListItem> GetCategories(GetCategoryListReq req)
        {
            int pageNumber = req.Pagination.PageNumber > 0 ? req.Pagination.PageNumber : 1;
            int pageSize   = req.Pagination.PageSize   > 0 ? req.Pagination.PageSize   : 10;
            int offset = (pageNumber - 1) * pageSize;

            string query = @"SELECT COUNT(*) OVER() AS TotalRecords, c.id, c.name, c.allowedUnitId, u.symbol AS allowedUnitSymbol FROM RawMaterialCategoryMaster c LEFT JOIN UnitMaster u ON c.allowedUnitId = u.id
                             WHERE c.isActive = 1 AND (@Name IS NULL OR c.name LIKE '%' + @Name + '%')
                             ORDER BY c.name OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            SqlParameter[] p = new SqlParameter[3];
            p[0] = new SqlParameter("@Name", Utilities.DbValue(req.Name));
            p[1] = new SqlParameter("@Offset", offset);
            p[2] = new SqlParameter("@PageSize", pageSize);

            DataSet ds = _client.ExecuteAdapter(query, p, CommandType.Text);

            PageResult<CategoryListItem> res = new PageResult<CategoryListItem>
            {
                TotalCount = 0,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                res.TotalCount = Convert.ToInt32(ds.Tables[0].Rows[0]["TotalRecords"]);

                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    CategoryListItem items = new CategoryListItem();
                    items.Edid = EncryptionHelper.Encrypt(row.Field<int>("id").ToString());
                    items.Name = row.Field<string>("name")!;
                    int? allowedUnitId = row.IsNull("allowedUnitId") ? null : row.Field<int>("allowedUnitId");
                    items.AllowedUnitEdid = allowedUnitId != null ? EncryptionHelper.Encrypt(allowedUnitId.Value.ToString()) : string.Empty;
                    items.AllowedUnitSymbol = row.Field<string?>("allowedUnitSymbol") ?? string.Empty;

                    res.Items.Add(items);
                }
            }

            return res;
        }

        public AddCategoryRes CreateCategory(AddCategoryReq req)
        {
            object unitIdVal = string.IsNullOrWhiteSpace(req.AllowedUnitEdid)
                ? DBNull.Value
                : (object)int.Parse(EncryptionHelper.Decrypt(req.AllowedUnitEdid));

            SqlParameter[] p = {
                new SqlParameter("@Name",          req.Name),
                new SqlParameter("@AllowedUnitId", unitIdVal)
            };

            string query = "INSERT INTO RawMaterialCategoryMaster (name,isActive,allowedUnitId) OUTPUT INSERTED.Id VALUES (@Name, 1, @AllowedUnitId)";
            int newId = _client.ExecuteScalar<int>(query, p, CommandType.Text);

            return new AddCategoryRes { Edid = EncryptionHelper.Encrypt(newId.ToString()) };
        }
        
        public PageResult<MaterialPurchaseHistoryItem> GetPurchaseHistory(string edid, GetMaterialPurchaseHistoryReq req)
        {
            string materialId = EncryptionHelper.Decrypt(edid);
            int pageNumber = req.Pagination.PageNumber > 0 ? req.Pagination.PageNumber : 1;
            int pageSize   = req.Pagination.PageSize   > 0 ? req.Pagination.PageSize   : 10;
            int offset = (pageNumber - 1) * pageSize;

            SqlParameter[] p = {
        new SqlParameter("@MaterialId", materialId),
        new SqlParameter("@Offset", offset),
        new SqlParameter("@PageSize", pageSize)
    };

            string countQuery = "SELECT COUNT(*) FROM PurchaseItem WHERE materialId = @MaterialId";
            SqlParameter[] cp = { new SqlParameter("@MaterialId", materialId) };
            int total = _client.ExecuteScalar<int>(countQuery, cp, CommandType.Text);

            string query = @"SELECT pm.id AS purchaseId, pm.publicNumber AS purchaseNumber, sm.name AS supplierName, mb.batchNumber,
                            pi.quantity AS qty, pi.rate, pm.date
                     FROM PurchaseItem pi
                     INNER JOIN PurchaseMaster pm ON pi.purchaseId = pm.id
                     INNER JOIN SupplierMaster sm ON pm.supplierId = sm.id
                     LEFT JOIN MaterialBatchMaster mb ON pi.materialBatchId = mb.id
                     WHERE pi.materialId = @MaterialId
                     ORDER BY pm.date DESC
                     OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            PageResult<MaterialPurchaseHistoryItem> res = new PageResult<MaterialPurchaseHistoryItem>
            {
                TotalCount = total,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            foreach (DataRow row in dt.Rows)
            {
                string purchaseId = Convert.ToString(row["purchaseId"]) ?? string.Empty;

                res.Items.Add(new MaterialPurchaseHistoryItem
                {
                    PurchaseNumber = row.Field<string>("purchaseNumber") ?? string.Empty,
                    PurchaseEdid = EncryptionHelper.Encrypt(purchaseId),
                    SupplierName = row.Field<string>("supplierName") ?? string.Empty,
                    BatchNumber = row.Field<string?>("batchNumber") ?? string.Empty,
                    Quantity = row.Field<decimal>("qty"),
                    UnitPrice = row.Field<decimal>("rate"),
                    PurchaseDate = row.Field<DateTime>("date")
                });
            }

            return res;
        }

        public MaterialUsageHistoryRes GetUsageHistory(GetMaterialUsageHistoryReq req)
        {
            string materialId = EncryptionHelper.Decrypt(req.MaterialEdid);
            string materialBatchId = EncryptionHelper.Decrypt(req.MaterialBatchEdid);

            MaterialUsageHistoryRes res = new MaterialUsageHistoryRes
            {
                Items = new List<MaterialUsageHistoryItem>()
            };

            if (string.IsNullOrWhiteSpace(materialId) || string.IsNullOrWhiteSpace(materialBatchId))
                return res;

            SqlParameter[] p = new SqlParameter[2];
            p[0] = new SqlParameter("@MaterialId", materialId);
            p[1] = new SqlParameter("@MaterialBatchId", materialBatchId);

            string query = @"SELECT remark, quantityUsed, createdAt
                     FROM MaterialBatchUsage
                     WHERE materialId = @MaterialId
                     AND materialBatchId = @MaterialBatchId
                     ORDER BY createdAt DESC";

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            foreach (DataRow row in dt.Rows)
            {
                res.Items.Add(new MaterialUsageHistoryItem
                {
                    Remark = row.Field<string>("remark") ?? string.Empty,
                    QuantityUsed = row.Field<decimal>("quantityUsed"),
                    UsageDate = row.Field<DateTime>("createdAt")
                });
            }

            return res;
        }

        public bool AddMaterialBatchUsage(MaterialBatchUsage req)
        {
            SqlParameter[] p = new SqlParameter[4];
            p[0] = new SqlParameter("@Remark", Utilities.DbValue(req.Remark));
            p[1] = new SqlParameter("@MaterialBatchId", EncryptionHelper.Decrypt(req.MaterialBatchEdid));
            p[2] = new SqlParameter("@QuantityUsed", req.QuantityUsed);
            p[3] = new SqlParameter("@MaterialId", EncryptionHelper.Decrypt(req.MaterialEdid));

            return _client.ExecuteScalar<bool>("AddMaterialBatchUsage", p, CommandType.StoredProcedure);
        }


        public PageResult<GetMaterialBatchListItem> GetMaterialBatchList(GetMaterialBatchListReq req)
        {
            string materialId = EncryptionHelper.Decrypt(req.materialEdid);

            if (string.IsNullOrWhiteSpace(materialId))
                return new PageResult<GetMaterialBatchListItem>();

            int pageNumber = req.Pagination.PageNumber > 0 ? req.Pagination.PageNumber : 1;
            int pageSize = req.Pagination.PageSize > 0 ? req.Pagination.PageSize : 10;
            int offset = (pageNumber - 1) * pageSize;

            SqlParameter[] p = new SqlParameter[4];
            p[0] = new SqlParameter("@MaterialId", materialId);
            p[1] = new SqlParameter("@Search", Utilities.DbValue(req.Search));
            p[2] = new SqlParameter("@Offset", offset);
            p[3] = new SqlParameter("@PageSize", pageSize);

            string query = @"SELECT mb.id, mb.batchNumber, COUNT(*) OVER() AS totalCount,
                            usageSummary.latestUsageDate, usageSummary.entriesCount, usageSummary.totalUsed
                     FROM MaterialBatchMaster mb
                     OUTER APPLY
                     (
                         SELECT MAX(mbu.createdAt) AS latestUsageDate,
                                COUNT(*) AS entriesCount,
                                COALESCE(SUM(mbu.quantityUsed), 0) AS totalUsed
                         FROM MaterialBatchUsage mbu
                         WHERE mbu.materialBatchId = mb.id
                     ) usageSummary
                     WHERE mb.materialId = @MaterialId
                     AND mb.isActive = 1
                     AND (@Search IS NULL OR mb.batchNumber LIKE '%' + @Search + '%')
                     ORDER BY mb.id DESC
                     OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            PageResult<GetMaterialBatchListItem> res = new PageResult<GetMaterialBatchListItem>
            {
                TotalCount = dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["totalCount"]) : 0,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            foreach (DataRow row in dt.Rows)
            {
                GetMaterialBatchListItem item = new GetMaterialBatchListItem();
                item.materialBatchEdid = EncryptionHelper.Encrypt(Convert.ToString(row["id"]));
                item.batchNumber = row.Field<string>("batchNumber") ?? string.Empty;
                item.latestUsageDate = row.Field<DateTime?>("latestUsageDate");
                item.entriesCount = row.Field<int>("entriesCount");
                item.totalUsed = row.Field<decimal>("totalUsed");

                res.Items.Add(item);
            }

            return res;
        }

        public GetPackingMaterialsRes GetPackingMaterials(string? searchKeyword = null)
        {
            string query = @"
                SELECT rm.id, rm.name, rc.name AS category, u.symbol AS allowedUnitSymbol, u.id AS unitId,
                       ISNULL(mb.stock, 0) AS stock
                FROM RawMaterialMaster rm
                INNER JOIN RawMaterialCategoryMaster rc ON rm.categoryId = rc.id
                LEFT JOIN UnitMaster u ON u.id = rc.allowedUnitId
                OUTER APPLY
                (
                    SELECT SUM(m.qtyRemaining) AS stock
                    FROM MaterialBatchMaster m
                    WHERE m.materialId = rm.id AND (m.isActive = 1 OR m.isActive IS NULL)
                ) mb
                WHERE (LOWER(rc.name) = 'packaging' OR LOWER(rc.name) = 'packing' OR LOWER(rc.name) LIKE '%pack%' OR LOWER(rc.name) LIKE '%box%')
                  AND (rm.isActive = 1 OR rm.isActive IS NULL)
                  AND (@SearchKeyword IS NULL OR rm.name LIKE '%' + @SearchKeyword + '%')
                ORDER BY rm.name ASC";

            SqlParameter[] p = { new SqlParameter("@SearchKeyword", Utilities.DbValue(searchKeyword)) };
            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];
            List<PackingMaterialItemDto> list = new List<PackingMaterialItemDto>();

            foreach (DataRow row in dt.Rows)
            {
                int id = row.Field<int>("id");
                int? unitId = row.IsNull("unitId") ? (int?)null : row.Field<int>("unitId");
                list.Add(new PackingMaterialItemDto
                {
                    Edid = EncryptionHelper.Encrypt(id.ToString()),
                    Name = row.Field<string>("name") ?? string.Empty,
                    Category = row.Field<string>("category") ?? string.Empty,
                    Stock = row.Field<decimal>("stock"),
                    UnitSymbol = row.Field<string?>("allowedUnitSymbol") ?? "pcs",
                    UnitEdid = unitId.HasValue ? EncryptionHelper.Encrypt(unitId.Value.ToString()) : null
                });
            }

            return new GetPackingMaterialsRes { Items = list };
        }
    }
}