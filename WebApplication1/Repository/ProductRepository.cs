using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using Microsoft.Data.SqlClient;
using System.Collections;
using System.Data;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Repository
{
    public interface IProductRepository
    {
        PageResult<ProductListItem> GetAllProduct(GetProductListReq req);
        GetProductByIdRes? GetById(string edid);
        string AddProduct(AddProductReq req, string? createdBy = null);
        void ToggleActive(string productEdid, bool active);
        GetComboItemsRes GetComboItems(string comboProductEdid);
        GetCombosUsingProductRes GetCombosUsingProduct(string productEdid, GetCombosUsingProductReq req);
        void SaveComboItems(string comboProductEdid, SaveComboItemsReq req);
        GetPricingRes GetPricing(string productEdid, CustomerType? customerType);
        void SavePricing(string productEdid, SavePricingReq req);
        GetPriceHistoryRes GetPriceHistory(GetPriceHistoryReq req);
        ValidateMaterialRes ValidateMaterial(string productEdid, ValidateMaterialReq req);
        ReconcileProductBatchRes ReconcileProductBatch(ReconcileProductBatchReq req);
        PageResult<CustomerProductListItem> GetProductsByCustomer(GetProductsByCustomerReq req);
        bool AddProductBatchMaster(ProductBatchEntry req);
        ProductUsageHistoryRes GetUsageHistory(GetProductUsageHistoryReq req);
        bool AddProductBatchUsage(ProductBatchUsage req);
        int CloseProductBatch(int productId, int productBatchId);
        PageResult<ProductBatchListItem> GetProductBatchList(GetProductBatchListReq req);
    }

    public class ProductRepository : IProductRepository
    {
        private readonly DbClient _client;
        private readonly IMaterialBatchUsageRepository _materialBatchUsageRepository;

        public ProductRepository(DbClient client, IMaterialBatchUsageRepository materialBatchUsageRepository)
        {
            _client = client;
            _materialBatchUsageRepository = materialBatchUsageRepository;
        }

        // ── Product ───────────────────────────────────────────────────────────
        public PageResult<ProductListItem> GetAllProduct(GetProductListReq req)
        {
            int offset = (req.Pagination.PageNumber - 1) * req.Pagination.PageSize;

            string query = @"SELECT COUNT(*) OVER() AS TotalRecords, p.id,p.name,p.qrCode,p.sku,p.qty,
                             CASE WHEN p.isCombo = 1 THEN ISNULL((SELECT MIN(FLOOR(ISNULL(c.stock, 0) / NULLIF(pci.quantity, 0))) 
                             FROM ProductComboItem pci
                             OUTER APPLY (SELECT SUM(pm.remainingQty) AS stock FROM ProductBatchMaster pm WHERE pm.productId = pci.productId AND pm.isActive = 1) c
                             WHERE pci.comboProductId = p.id), 0)
                             ELSE ISNULL((SELECT SUM(pb.remainingQty) FROM ProductBatchMaster pb WHERE pb.productId = p.id AND pb.isActive = 1), 0) END AS stock,
                             p.isActive,p.isCombo FROM ProductMaster p WHERE (@IsActive IS NULL OR p.isActive = @IsActive)
                             AND (@IsCombo IS NULL OR p.isCombo = @IsCombo)
                             AND (@SearchKeyword IS NULL OR p.name LIKE '%' + @SearchKeyword + '%' OR p.sku LIKE '%' + @SearchKeyword + '%' OR p.qrCode LIKE '%' + @SearchKeyword + '%')
                             ORDER BY p.id DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            SqlParameter[] p = new SqlParameter[5];
            p[0] = new SqlParameter("@Offset", offset);
            p[1] = new SqlParameter("@PageSize", req.Pagination.PageSize);
            p[2] = new SqlParameter("@IsActive", Utilities.DbValue<bool>(req.IsActive));
            p[3] = new SqlParameter("@SearchKeyword", Utilities.DbValue(req.SearchKeyword));
            p[4] = new SqlParameter("@IsCombo", Utilities.DbValue<bool>(req.IsCombo));

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            PageResult<ProductListItem> res = new PageResult<ProductListItem>
            {
                TotalCount = dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["TotalRecords"]) : 0,
                PageNumber = req.Pagination.PageNumber,
                PageSize = req.Pagination.PageSize
            };

            foreach (DataRow row in dt.Rows)
            {
                ProductListItem items = new ProductListItem();
                items.Edid = EncryptionHelper.Encrypt(row.Field<int>("id").ToString());
                items.Name = row.Field<string>("name")!;
                items.Sku = row.Field<string?>("sku");
                items.IsActive = row.Field<bool>("isActive");
                items.Stock = Convert.ToDecimal(row["stock"]);
                items.Qty = row.IsNull("qty") ? null : Convert.ToDecimal(row["qty"]);
                items.IsCombo = row.Field<bool>("isCombo");

                res.Items.Add(items);
            }

            return res;
        }

        public GetProductByIdRes? GetById(string edid)
        {
            SqlParameter[] p = { new SqlParameter("@Id", EncryptionHelper.Decrypt(edid)) };

            string query = @"SELECT p.id,p.name,p.sku,p.qty,
                               CASE WHEN p.isCombo = 1 THEN 
                                   ISNULL((SELECT MIN(FLOOR(ISNULL(bs.stock, 0) / pci.quantity))
                                           FROM ProductComboItem pci
                                           LEFT JOIN (SELECT productId, SUM(remainingQty) AS stock
                                                      FROM ProductBatchMaster
                                                      WHERE isActive = 1
                                                      GROUP BY productId) bs ON pci.productId = bs.productId
                                           WHERE pci.comboProductId = p.id), 0)
                               ELSE ISNULL((SELECT SUM(pb.remainingQty)
                                            FROM ProductBatchMaster pb
                                            WHERE pb.productId = p.id AND pb.isActive = 1), 0) END as stock,
                               p.isActive,p.isCombo
                        FROM ProductMaster p
                        WHERE p.id=@Id";

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            return new GetProductByIdRes
            {
                Edid = EncryptionHelper.Encrypt(row.Field<int>("id").ToString()),
                Name = row.Field<string>("name")!,
                Sku = row.Field<string?>("sku"),
                Stock = Convert.ToDecimal(row["stock"]),
                Qty = row.IsNull("qty") ? null : Convert.ToDecimal(row["qty"]),
                Active = row.Field<bool>("isActive"),
                IsCombo = row.Field<bool>("isCombo")
            };
        }

        public string AddProduct(AddProductReq req, string? createdBy = null)
        {
            decimal? productQty = req.IsCombo ? (decimal?)null : (req.Qty.HasValue && req.Qty.Value > 0 ? req.Qty.Value : 1m);

            SqlParameter[] p = new SqlParameter[6];
            p[0] = new SqlParameter("@Name", req.Name);
            p[1] = new SqlParameter("@QrCode", req.QrCode);
            p[2] = new SqlParameter("@Sku", Utilities.DbValue(req.Sku));
            p[3] = new SqlParameter("@IsCombo", req.IsCombo);
            p[4] = new SqlParameter("@CreatedBy", Utilities.DbValue(createdBy));
            p[5] = new SqlParameter("@Qty", Utilities.DbValue(productQty));

            string query = @"INSERT INTO ProductMaster (name,qrCode,sku,isCombo,qty,createdBy,createdAt) OUTPUT INSERTED.Id VALUES (@Name,@QrCode,@Sku,@IsCombo,@Qty,@CreatedBy,GETUTCDATE())";

            string newId = _client.ExecuteScalar<int>(query, p, CommandType.Text).ToString();

            if (req.IsCombo && req.ComboItems != null && req.ComboItems.Count > 0)
            {
                foreach (ComoboItems item in req.ComboItems)
                {
                    SqlParameter[] cp = new SqlParameter[3];
                    cp[0] = new SqlParameter("@ComboProductId", newId);
                    cp[1] = new SqlParameter("@ProductId", EncryptionHelper.Decrypt(item.ProductEdid));
                    cp[2] = new SqlParameter("@Quantity", item.Quantity);

                    _client.ExecuteNonQuery(@"INSERT INTO ProductComboItem (comboProductId,productId,quantity) VALUES (@ComboProductId,@ProductId,@Quantity)", cp, CommandType.Text);
                }
            }

            return newId;
        }

        public void ToggleActive(string productEdid, bool active)
        {
            string productId = EncryptionHelper.Decrypt(productEdid);
            SqlParameter[] p = { new SqlParameter("@ProductId", productId) };

            if (active)
            {
                _client.ExecuteNonQuery("UPDATE ProductMaster SET isActive=1 WHERE id=@ProductId", p, CommandType.Text);
            }
            else
            {
                _client.ExecuteNonQuery("UPDATE ProductMaster SET isActive=0 WHERE id=@ProductId", p, CommandType.Text);

                string query = @"DECLARE @IsCombo BIT;
                                SELECT @IsCombo = isCombo FROM ProductMaster WHERE id=@ProductId;

                                IF @IsCombo = 0 BEGIN UPDATE ProductMaster SET isActive = 0 
                                WHERE id IN (SELECT comboProductId FROM ProductComboItem WHERE productId=@ProductId) END";

                SqlParameter[] p2 = { new SqlParameter("@ProductId", productId) };
                _client.ExecuteNonQuery(query, p2, CommandType.Text);
            }
        }


        // ── Combo Items ───────────────────────────────────────────────────────
        public GetComboItemsRes GetComboItems(string comboProductEdid)
        {
            string comboProductId = EncryptionHelper.Decrypt(comboProductEdid);
            SqlParameter[] p = { new SqlParameter("@ComboProductId", comboProductId) };

            string query = @"WITH ProductStock AS
                     (
                         SELECT p.id, p.name, p.sku, p.isCombo, p.isActive,
                                ISNULL(SUM(b.remainingQty), 0) AS stock
                         FROM ProductMaster p
                         LEFT JOIN ProductBatchMaster b ON b.productId = p.id AND b.isActive = 1
                         GROUP BY p.id, p.name, p.sku, p.isCombo, p.isActive
                     ),
                     ComboStock AS
                     (
                         SELECT pci.comboProductId,
                                MIN(FLOOR(ps.stock / NULLIF(pci.quantity, 0))) AS stock
                         FROM ProductComboItem pci
                         INNER JOIN ProductStock ps ON pci.productId = ps.id
                         GROUP BY pci.comboProductId
                     )
                     SELECT c.id, c.productId, c.quantity AS unitCount, p.name AS productName, p.sku,
                            CASE WHEN p.isCombo = 1 THEN ISNULL(cs.stock, 0)
                                 ELSE ps.stock END AS stock,
                            p.isActive, pp.customerType, pp.price, pp.mrp
                     FROM ProductComboItem c
                     INNER JOIN ProductStock ps ON c.productId = ps.id
                     INNER JOIN ProductMaster p ON c.productId = p.id
                     LEFT JOIN ComboStock cs ON cs.comboProductId = p.id
                     LEFT JOIN ProductPricing pp ON c.productId = pp.productId AND pp.isActive = 1
                     WHERE c.comboProductId = @ComboProductId
                     ORDER BY c.id, pp.customerType";

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            GetComboItemsRes res = new GetComboItemsRes();
            Dictionary<int, ComboItemLine> dict = new Dictionary<int, ComboItemLine>();

            foreach (DataRow row in dt.Rows)
            {
                int cId = row.Field<int>("id");

                if (!dict.TryGetValue(cId, out ComboItemLine? comboItem))
                {
                    comboItem = new ComboItemLine
                    {
                        Edid = EncryptionHelper.Encrypt(cId.ToString()),
                        ProductEdid = EncryptionHelper.Encrypt(row.Field<int>("productId").ToString()),
                        ProductName = row.Field<string>("productName")!,
                        Sku = row.Field<string?>("sku"),
                        Stock = Convert.ToDecimal(row["stock"]),
                        Active = row.Field<bool>("isActive"),
                        UnitCount = Convert.ToInt32(row["unitCount"])
                    };

                    dict[cId] = comboItem;
                    res.Items.Add(comboItem);
                }

                if (!row.IsNull("customerType"))
                {
                    string customerType = row.Field<string>("customerType")!;

                    PricingLine? pricingLine = comboItem.Prize.FirstOrDefault(x => x.customerType == customerType);

                    if (pricingLine == null)
                    {
                        pricingLine = new PricingLine
                        {
                            customerType = customerType,
                            Mrp = Convert.ToDecimal(row["mrp"])
                        };

                        comboItem.Prize.Add(pricingLine);
                    }

                    pricingLine.Price.Add(new ProductPriceItem
                    {
                        CustomerType = customerType,
                        Price = Convert.ToDecimal(row["price"]),
                        Mrp = Convert.ToDecimal(row["mrp"])
                    });
                }
            }

            return res;
        }

        public GetCombosUsingProductRes GetCombosUsingProduct(string productEdid, GetCombosUsingProductReq req)
        {
            string productId = EncryptionHelper.Decrypt(productEdid);
            int offset = (req.PageNumber - 1) * req.PageSize;

            SqlParameter[] p = new SqlParameter[3];
            p[0] = new SqlParameter("@ProductId", productId);
            p[1] = new SqlParameter("@Offset", offset);
            p[2] = new SqlParameter("@PageSize", req.PageSize);

            string query = @"SELECT COUNT(*) OVER() AS TotalRecords, pm.id, pm.name, pm.isActive, pci.quantity,
                            pp.customerType, pp.price, pp.mrp
                     FROM ProductComboItem pci
                     INNER JOIN ProductMaster pm ON pci.comboProductId = pm.id
                     LEFT JOIN ProductPricing pp ON pp.productId = pm.id AND pp.isActive = 1
                     WHERE pci.productId = @ProductId
                     ORDER BY pm.id DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            GetCombosUsingProductRes res = new GetCombosUsingProductRes
            {
                TotalCount = dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["TotalRecords"]) : 0,
                PageNumber = req.PageNumber,
                PageSize = req.PageSize
            };

            foreach (DataRow row in dt.Rows)
            {
                string comboProductEdid = EncryptionHelper.Encrypt(row.Field<int>("id").ToString());
                CombosUsingProductItem? item = res.Items.FirstOrDefault(x => x.ComboProductEdid == comboProductEdid);

                if (item == null)
                {
                    item = new CombosUsingProductItem
                    {
                        ComboProductEdid = comboProductEdid,
                        ComboProductName = row.Field<string>("name")!,
                        Active = row.Field<bool>("isActive"),
                        UnitCount = Convert.ToInt32(row["quantity"])
                    };
                    res.Items.Add(item);
                }

                if (!row.IsNull("customerType"))
                {
                    item.Prices.Add(new ProductPriceItem
                    {
                        CustomerType = row.Field<string>("customerType")!,
                        Price = Convert.ToDecimal(row["price"]),
                        Mrp = Convert.ToDecimal(row["mrp"])
                    });
                }
            }

            return res;
        }

        public void SaveComboItems(string comboProductEdid, SaveComboItemsReq req)
        {
            string comboProductId = EncryptionHelper.Decrypt(comboProductEdid);

            SqlParameter[] del = { new SqlParameter("@ComboProductId", comboProductId) };
            _client.ExecuteNonQuery("DELETE FROM ProductComboItem WHERE comboProductId=@ComboProductId", del, CommandType.Text);

            foreach (SaveComboItemLine item in req.Items)
            {
                string productId = EncryptionHelper.Decrypt(item.ProductEdid);
                    
                SqlParameter[] p = new SqlParameter[3];
                p[0] = new SqlParameter("@ComboProductId", comboProductId);
                p[1] = new SqlParameter("@ProductId",      productId);
                p[2] = new SqlParameter("@UnitCount",      item.UnitCount);

                _client.ExecuteNonQuery("INSERT INTO ProductComboItem (comboProductId,productId,quantity) VALUES (@ComboProductId,@ProductId,@UnitCount)", p, CommandType.Text);
            }
        }

        // ── Pricing ───────────────────────────────────────────────────────────

        public GetPricingRes GetPricing(string productEdid, CustomerType? customerType)
        {
            string productId = EncryptionHelper.Decrypt(productEdid);

            SqlParameter[] p = new SqlParameter[2];

            p[0] = new SqlParameter("@ProductId", productId);
            p[1] = new SqlParameter("@customerType", Utilities.DbValue(customerType?.ToString()));

            string query = @"SELECT customerType, price, mrp
                     FROM ProductPricing
                     WHERE productId = @ProductId
                     AND isActive = 1
                     AND (@customerType IS NULL OR customerType = @customerType)
                     ORDER BY customerType";

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            GetPricingRes res = new GetPricingRes();

            foreach (DataRow row in dt.Rows)
            {
                string type = row.Field<string>("customerType")!;

                PricingLine? pricingLine = res.Items.FirstOrDefault(x => x.customerType == type);

                if (pricingLine == null)
                {
                    pricingLine = new PricingLine
                    {
                        customerType = type,
                        Mrp = Convert.ToDecimal(row["mrp"])
                    };

                    res.Items.Add(pricingLine);
                }

                pricingLine.Price.Add(new ProductPriceItem
                {
                    CustomerType = type,
                    Price = Convert.ToDecimal(row["price"]),
                    Mrp = Convert.ToDecimal(row["mrp"])
                });
            }

            return res;
        }
        public void SavePricing(string productEdid, SavePricingReq req)
        {
            string productId = EncryptionHelper.Decrypt(productEdid);
            string priceGroupId = Utilities.GenerateRandomNumber(7);

            SqlParameter[] upd = { new SqlParameter("@ProductId", productId) };
            string query = "UPDATE ProductPricing SET isActive=0 WHERE productId=@ProductId";
            _client.ExecuteNonQuery(query, upd, CommandType.Text);

            foreach (SavePricingLine pricing in req.Items)
            {
                SqlParameter[] p = new SqlParameter[5];
                p[0] = new SqlParameter("@ProductId", productId);
                p[1] = new SqlParameter("@CustomerType", pricing.CustomerType.ToString());
                p[2] = new SqlParameter("@Price", pricing.Price);
                p[3] = new SqlParameter("@Mrp", pricing.Mrp);
                p[4] = new SqlParameter("@PriceGroupId", priceGroupId);

                query = "INSERT INTO ProductPricing (productId,customerType,price,mrp,priceGroupId) " +
                        "VALUES (@ProductId,@CustomerType,@Price,@Mrp,@PriceGroupId)";

                _client.ExecuteNonQuery(query, p, CommandType.Text);
            }

            // Deactivate combo products containing this product when its price changes
            SqlParameter[] deact = { new SqlParameter("@ProductId", productId) };
            string deactQuery = @"UPDATE ProductMaster SET isActive = 0 WHERE id IN (SELECT comboProductId FROM ProductComboItem WHERE productId = @ProductId)";
            _client.ExecuteNonQuery(deactQuery, deact, CommandType.Text);
        }


        public GetPriceHistoryRes GetPriceHistory(GetPriceHistoryReq req)
        {
            string productId = EncryptionHelper.Decrypt(req.ProductEdid);
            int offset = (req.PageNumber - 1) * req.PageSize;

            string query = @"WITH GroupedCTE AS (
                        SELECT priceGroupId, MAX(createdAt) as maxCreatedAt, MAX(CAST(isActive AS INT)) as grpIsActive
                        FROM ProductPricing
                        WHERE productId=@ProductId
                        GROUP BY priceGroupId
                     ),
                     PagedCTE AS (
                        SELECT priceGroupId, maxCreatedAt, grpIsActive,
                               COUNT(*) OVER() AS TotalRecords
                        FROM GroupedCTE
                        ORDER BY maxCreatedAt DESC
                        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
                     )
                     SELECT p.priceGroupId, p.customerType, p.price, p.mrp, 
                            c.maxCreatedAt as createdAt, CAST(c.grpIsActive AS BIT) as isActive,
                            c.TotalRecords
                     FROM PagedCTE c
                     INNER JOIN ProductPricing p ON c.priceGroupId = p.priceGroupId AND p.productId=@ProductId
                     ORDER BY c.maxCreatedAt DESC";

            SqlParameter[] p = new SqlParameter[3];
            p[0] = new SqlParameter("@ProductId", productId);
            p[1] = new SqlParameter("@Offset", offset);
            p[2] = new SqlParameter("@PageSize", req.PageSize);

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            GetPriceHistoryRes res = new GetPriceHistoryRes
            {
                TotalCount = dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["TotalRecords"]) : 0,
                PageNumber = req.PageNumber,
                PageSize = req.PageSize
            };

            foreach (DataRow row in dt.Rows)
            {
                string pgId = row.IsNull("priceGroupId") ? "" : Convert.ToString(row["priceGroupId"])!;
                var group = res.Items.FirstOrDefault(g => g.PriceGroupId == pgId);

                if (group == null)
                {
                    group = new PriceHistoryGroup
                    {
                        PriceGroupId = pgId,
                        CreatedAt = row.Table.Columns.Contains("createdAt") && !row.IsNull("createdAt") ? row.Field<DateTime>("createdAt") : null,
                        IsActive = row.Table.Columns.Contains("isActive") && !row.IsNull("isActive") ? row.Field<bool>("isActive") : false
                    };
                    res.Items.Add(group);
                }

                group.Details.Add(new PriceHistoryLine
                {
                    customerType = row.Field<string>("customerType")!,
                    Price = row.Field<decimal>("price"),
                    Mrp = row.Field<decimal>("mrp")
                });
            }

            return res;
        }

        public ValidateMaterialRes ValidateMaterial(string productEdid, ValidateMaterialReq req)
        {
            int productId = int.Parse(EncryptionHelper.Decrypt(productEdid));
            int materialId = int.Parse(EncryptionHelper.Decrypt(req.MaterialEdid));
            int batchId = int.Parse(EncryptionHelper.Decrypt(req.BatchEdid));

            // Get recipe qty
            string recipeQuery = "SELECT qty FROM RecipeItem WHERE productId = @ProductId AND materialId = @MaterialId";
            SqlParameter[] recipeParams = new SqlParameter[2];
            recipeParams[0] = new SqlParameter("@ProductId", productId);
            recipeParams[1] = new SqlParameter("@MaterialId", materialId);
            
            DataTable recipeDt = _client.ExecuteAdapter(recipeQuery, recipeParams, CommandType.Text).Tables[0];
            if (recipeDt.Rows.Count == 0)
                throw new HoneyErpException(ErrorCode.NotFound, "Material is not in the product recipe.");
                
            decimal recipeQty = recipeDt.Rows[0].Field<decimal>("qty");
            decimal requiredQty = recipeQty * req.NewStockValue;

            // Get batch remaining qty
            string batchQuery = "SELECT qtyRemaining FROM MaterialBatchMaster WHERE id = @BatchId AND materialId = @MaterialId AND batchNumber = @BatchNumber";
            SqlParameter[] batchParams = new SqlParameter[3];
            batchParams[0] = new SqlParameter("@BatchId", batchId);
            batchParams[1] = new SqlParameter("@MaterialId", materialId);
            batchParams[2] = new SqlParameter("@BatchNumber", Utilities.DbValue(req.BatchNumber));
            
            DataTable batchDt = _client.ExecuteAdapter(batchQuery, batchParams, CommandType.Text).Tables[0];
            if (batchDt.Rows.Count == 0)
                throw new HoneyErpException(ErrorCode.NotFound, "Material batch not found.");
                
            decimal availableQty = batchDt.Rows[0].Field<decimal>("qtyRemaining");

            return new ValidateMaterialRes
            {
                IsValid = availableQty >= requiredQty,
                RequiredQty = requiredQty,
                AvailableQty = availableQty
            };
        }

        // ── Product Batch Reconciliation ──────────────────────────────────────

        public ReconcileProductBatchRes ReconcileProductBatch(ReconcileProductBatchReq req)
        {
            int productId = int.Parse(EncryptionHelper.Decrypt(req.ProductEdid));

            // ── Resolve product batch ────────────────────────────────────────────
            int     productBatchId;
            string  resolvedBatchNumber;
            decimal existingRemainingQty;

            bool isNewBatch = string.IsNullOrWhiteSpace(req.BatchNumber);

            if (!isNewBatch)
            {
                // Existing batch – look it up
                int batchId = int.Parse(EncryptionHelper.Decrypt(req.ProductBatchEdid!));

                SqlParameter[] pbP = new SqlParameter[2];
                pbP[0] = new SqlParameter("@BatchId",   batchId);
                pbP[1] = new SqlParameter("@ProductId", productId);

                string pbQuery = "SELECT id, batchNumber, remainingQty FROM ProductBatchMaster WHERE id = @BatchId AND productId = @ProductId";
                DataTable pbDt = _client.ExecuteAdapter(pbQuery, pbP, CommandType.Text).Tables[0];

                productBatchId       = pbDt.Rows[0].Field<int>("id");
                resolvedBatchNumber  = pbDt.Rows[0].Field<string>("batchNumber")!;
                existingRemainingQty = pbDt.Rows[0].Field<decimal>("remainingQty");

            }
            else
            {
                // New batch – auto-generate batch number and insert
                string date         = DateTime.Now.ToString("yyMMdd");
                string randPart     = Utilities.GenerateRandomNumber(3);
                resolvedBatchNumber  = $"PB-{date}{randPart}";
                existingRemainingQty = 0;

                SqlParameter[] newBP = new SqlParameter[7];
                newBP[0] = new SqlParameter("@BatchNumber",   resolvedBatchNumber);
                newBP[1] = new SqlParameter("@ProductId",     productId);
                newBP[2] = new SqlParameter("@ProductionQty", req.ProductionQty);
                newBP[3] = new SqlParameter("@RemainingQty",  0);
                newBP[4] = new SqlParameter("@Remarks",       Utilities.DbValue(req.Remarks));
                newBP[5] = new SqlParameter("@PackedDate", Utilities.DbValue(req.PackedDate));
                newBP[6] = new SqlParameter("@ExpiryDate", Utilities.DbValue(req.ExpiryDate));

                string newBatchQuery = @"INSERT INTO ProductBatchMaster
                        (batchNumber, productId, productionQty, remainingQty, isActive, remarks, createdAt, packedDate, expiryDate)
                      OUTPUT INSERTED.id
                      VALUES (@BatchNumber, @ProductId, @ProductionQty, @RemainingQty, 1, @Remarks, GETDATE(), @PackedDate, @ExpiryDate)";
                productBatchId = _client.ExecuteScalar<int>(newBatchQuery, newBP, CommandType.Text);
            }

            // ── Write usage + deduct stock for each material ─────────────────────
            foreach (ReconcileMaterialLine mat in req.Materials)
            {
                int materialId      = int.Parse(EncryptionHelper.Decrypt(mat.MaterialEdid));
                int materialBatchId = int.Parse(EncryptionHelper.Decrypt(mat.MaterialBatchEdid));
                // Get recipe qty and id to recalculate actual usage and log usage
                SqlParameter[] rqP = new SqlParameter[2];
                rqP[0] = new SqlParameter("@ProductId",  productId);
                rqP[1] = new SqlParameter("@MaterialId", materialId);
                string recipeQuery = "SELECT id, qty FROM RecipeItem WHERE productId = @ProductId AND materialId = @MaterialId AND (isActive = 1 OR isActive IS NULL)";
                DataTable recipeDt = _client.ExecuteAdapter(recipeQuery, rqP, CommandType.Text).Tables[0];
                decimal recipeQty = recipeDt.Rows.Count > 0 ? recipeDt.Rows[0].Field<decimal>("qty") : 1m;
                decimal actualQtyUsed = recipeQty * req.ProductionQty;

                bool recorded = _materialBatchUsageRepository.Add(
                    materialId,
                    materialBatchId,
                    actualQtyUsed,
                    MaterialBatchEntryType.Out,
                    $"Batch #{resolvedBatchNumber} production consumed {actualQtyUsed}");
                if (!recorded)
                    throw new HoneyErpException(ErrorCode.InvalidData, "Insufficient material batch stock for production.");
            }

            if (!RecordProductBatchUsage(
                    productId,
                    productBatchId,
                    req.ProductionQty,
                    MaterialBatchEntryType.In,
                    req.Remarks ?? $"Batch #{resolvedBatchNumber} production received {req.ProductionQty}"))
            {
                throw new HoneyErpException(ErrorCode.InvalidData, "Unable to record produced product batch quantity.");
            }

            // ── Insert ProductReconciliation ──────────────────────────────────────
            SqlParameter[] reconP = new SqlParameter[6];
            reconP[0] = new SqlParameter("@ProductBatchId", productBatchId);
            reconP[1] = new SqlParameter("@AdjustmentQty",  req.ProductionQty);
            reconP[2] = new SqlParameter("@AdjustmentType", "Add");
            reconP[3] = new SqlParameter("@Reason",         Utilities.DbValue(req.Reason));
            reconP[4] = new SqlParameter("@Remarks",        Utilities.DbValue(req.Remarks));
            reconP[5] = new SqlParameter("@CreatedAt",      DateTime.Now);
            string reconQuery = @"INSERT INTO ProductReconciliation
                    (productBatchId, adjustmentQty, adjustmentType, reason, remarks, createdAt)
                  VALUES (@ProductBatchId, @AdjustmentQty, @AdjustmentType, @Reason, @Remarks, @CreatedAt)";
            _client.ExecuteNonQuery(reconQuery, reconP, CommandType.Text);

            return new ReconcileProductBatchRes
            {
                ProductBatchEdid = EncryptionHelper.Encrypt(productBatchId.ToString()),
                BatchNumber      = resolvedBatchNumber,
                ProductionQty    = req.ProductionQty,
                RemainingQty     = isNewBatch ? req.ProductionQty : existingRemainingQty + req.ProductionQty
            };
        }

        public PageResult<CustomerProductListItem> GetProductsByCustomer(GetProductsByCustomerReq req)
        {
            int pageNumber = req.Pagination.PageNumber > 0 ? req.Pagination.PageNumber : 1;
            int pageSize = req.Pagination.PageSize > 0 ? req.Pagination.PageSize : 20;
            int offset = (pageNumber - 1) * pageSize;

            PageResult<CustomerProductListItem> res = new PageResult<CustomerProductListItem>
            {
                TotalCount = 0,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            string query = @"DECLARE @CustType VARCHAR(50) = NULL;

        SELECT TOP 1 @CustType = UPPER(LTRIM(RTRIM(type))) FROM CustomerMaster WHERE id = @CustomerId;

        IF @CustType IS NULL OR @CustType = ''
        BEGIN
            SELECT 0 AS TotalRecords WHERE 1 = 0;
        END
        ELSE
        BEGIN
            WITH CustPricing AS (SELECT productId, price, mrp, customerType,ROW_NUMBER() OVER (PARTITION BY productId ORDER BY id DESC) as rn
                   FROM ProductPricing WHERE UPPER(LTRIM(RTRIM(customerType))) = @CustType AND isActive = 1)

            SELECT COUNT(*) OVER() AS TotalRecords,p.id,p.name,p.sku,p.qrCode,p.isActive AS isActive,p.isCombo,cp.price,cp.mrp,cp.customerType,
                   CASE WHEN p.isCombo = 1 THEN ISNULL((SELECT MIN(FLOOR(ISNULL(c.stock, 0) / NULLIF(pci.quantity, 0))) FROM ProductComboItem pci
                   OUTER APPLY (SELECT SUM(pm.remainingQty) AS stock FROM ProductBatchMaster pm WHERE pm.productId = pci.productId AND pm.isActive = 1) c WHERE pci.comboProductId = p.id), 0)
                   ELSE ISNULL((SELECT SUM(pb.remainingQty) FROM ProductBatchMaster pb WHERE pb.productId = p.id AND pb.isActive = 1), 0) END AS stock
            FROM ProductMaster p
            INNER JOIN CustPricing cp ON cp.productId = p.id AND cp.rn = 1 WHERE p.isActive = 1 AND (@IsCombo IS NULL OR p.isCombo = @IsCombo)
              AND (@SearchKeyword IS NULL OR p.name LIKE '%' + @SearchKeyword + '%' OR p.sku LIKE '%' + @SearchKeyword + '%' OR p.qrCode LIKE '%' + @SearchKeyword + '%')
            ORDER BY p.name ASC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
        END";

            SqlParameter[] p = new SqlParameter[5];
            p[0] = new SqlParameter("@CustomerId", EncryptionHelper.Decrypt(req.CustomerEdid));
            p[1] = new SqlParameter("@Offset", offset);
            p[2] = new SqlParameter("@PageSize", pageSize);
            p[3] = new SqlParameter("@IsCombo", Utilities.DbValue<bool>(req.IsCombo));
            p[4] = new SqlParameter("@SearchKeyword", Utilities.DbValue(req.SearchKeyword));

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            if (dt.Rows.Count > 0)
            {
                res.TotalCount = Convert.ToInt32(dt.Rows[0]["TotalRecords"]);

                foreach (DataRow row in dt.Rows)
                {
                    string edid = EncryptionHelper.Encrypt(row.Field<int>("id").ToString());

                    res.Items.Add(new CustomerProductListItem
                    {
                        Edid = edid,
                        Id = edid,
                        Name = row.Field<string>("name") ?? string.Empty,
                        Sku = row.Field<string?>("sku"),
                        Stock = Convert.ToDecimal(row["stock"]),
                        IsCombo = row.Field<bool>("isCombo"),
                        IsActive = row.Field<bool>("isActive"),
                        ProductPricing = new ProductPriceItem
                        {
                            PriceItemEdid = EncryptionHelper.Encrypt(row.Field<int>("id").ToString()),
                            CustomerType = row.Field<string>("customerType") ?? string.Empty,
                            Price = Convert.ToDecimal(row["price"]),
                            Mrp = Convert.ToDecimal(row["mrp"])
                        },
                        CustomerType = Utilities.ParseEnum<CustomerType>(row.Field<string>("customerType"))
                    });
                }
            }

            return res;
        }


        public bool AddProductBatchMaster(ProductBatchEntry req)
        {
            int productId = EncryptionHelper.Decrypt<int>(req.ProductEdid);
            SqlParameter[] parameters =
            {
                new("@BatchNumber", Utilities.DbValue(req.BatchNumber)),
                new("@ProductId", productId),
                new("@ProductionQty", req.ProducedQty),
                new("@RemainingQty", req.AvailableQty),
                new("@PackedDate", req.PackedDate),
                new("@ExpiryDate", req.ExpiryDate),
                new("@Remark", Utilities.DbValue(req.Remark))
            };

            const string query = @"
                SET NOCOUNT ON;
                BEGIN TRY
                    BEGIN TRANSACTION;

                    DECLARE @ProductBatchId INT;
                    INSERT INTO ProductBatchMaster
                        (batchNumber, productId, productionQty, remainingQty, isActive, remarks, packedDate, expiryDate)
                    VALUES
                        (@BatchNumber, @ProductId, @ProductionQty, @RemainingQty, 1, @Remark, @PackedDate, @ExpiryDate);
                    SET @ProductBatchId = SCOPE_IDENTITY();

                    INSERT INTO ProductBatchUsage
                        (remark, productBatchId, quantityUsed, productId, entryType)
                    VALUES
                        (@Remark, @ProductBatchId, @RemainingQty, @ProductId, 'In');

                    COMMIT TRANSACTION;
                    SELECT CAST(1 AS INT);
                END TRY
                BEGIN CATCH
                    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                    THROW;
                END CATCH;";

            return _client.ExecuteScalar<int>(query, parameters, CommandType.Text) == 1;
        }

        public ProductUsageHistoryRes GetUsageHistory(GetProductUsageHistoryReq req)
        {
            string productId = EncryptionHelper.Decrypt(req.ProductEdid);
            string productBatchId = EncryptionHelper.Decrypt(req.ProductBatchEdid);

            ProductUsageHistoryRes res = new ProductUsageHistoryRes
            {
                Items = new List<ProductUsageHistoryItem>()
            };

            if (string.IsNullOrWhiteSpace(productId) || string.IsNullOrWhiteSpace(productBatchId))
                return res;

            SqlParameter[] p = new SqlParameter[3];
            p[0] = new SqlParameter("@ProductId", productId);
            p[1] = new SqlParameter("@ProductBatchId", productBatchId);
            p[2] = new SqlParameter("@SearchRemark", Utilities.DbValue(req.SearchRemark));

            string query = @"SELECT remark, quantityUsed, entryType, createdAt
                     FROM ProductBatchUsage
                     WHERE productId = @ProductId
                     AND productBatchId = @ProductBatchId
                     AND (@SearchRemark IS NULL OR remark LIKE '%' + @SearchRemark + '%')
                     ORDER BY createdAt DESC";

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            foreach (DataRow row in dt.Rows)
            {
                res.Items.Add(new ProductUsageHistoryItem
                {
                    Remark = row.Field<string>("remark") ?? string.Empty,
                    QuantityUsed = row.Field<decimal>("quantityUsed"),
                    EntryType = Enum.TryParse(row.Field<string?>("entryType"), true, out MaterialBatchEntryType entryType)
                        ? entryType
                        : MaterialBatchEntryType.Out,
                    UsageDate = row.Field<DateTime>("createdAt")
                });
            }

            return res;
        }

        public bool AddProductBatchUsage(ProductBatchUsage req)
        {
            if (req.QuantityUsed <= 0 || !Enum.IsDefined(req.EntryType))
                return false;

            int productBatchId = EncryptionHelper.Decrypt<int>(req.ProductBatchEdid);
            int productId = EncryptionHelper.Decrypt<int>(req.ProductEdid);
            return RecordProductBatchUsage(productId, productBatchId, req.QuantityUsed, req.EntryType, req.Remark);
        }

        public int CloseProductBatch(int productId, int productBatchId)
        {
            SqlParameter[] parameters =
            {
                new("@ProductId", productId),
                new("@ProductBatchId", productBatchId)
            };

            const string query = @"
                SET NOCOUNT ON;
                BEGIN TRY
                    BEGIN TRANSACTION;

                    DECLARE @IsActive BIT;
                    DECLARE @RemainingQty DECIMAL(18, 4);
                    DECLARE @Found BIT = 0;

                    SELECT @IsActive = isActive, @RemainingQty = remainingQty
                    FROM ProductBatchMaster WITH (UPDLOCK, HOLDLOCK)
                    WHERE id = @ProductBatchId AND productId = @ProductId;
                    IF @@ROWCOUNT > 0 SET @Found = 1;

                    IF @Found = 0
                    BEGIN
                        ROLLBACK TRANSACTION;
                        SELECT CAST(0 AS INT);
                        RETURN;
                    END;

                    IF @IsActive = 0
                    BEGIN
                        ROLLBACK TRANSACTION;
                        SELECT CAST(-1 AS INT);
                        RETURN;
                    END;

                    IF ISNULL(@RemainingQty, 0) <> 0
                    BEGIN
                        ROLLBACK TRANSACTION;
                        SELECT CAST(-2 AS INT);
                        RETURN;
                    END;

                    UPDATE ProductBatchMaster SET isActive = 0
                    WHERE id = @ProductBatchId AND productId = @ProductId;

                    COMMIT TRANSACTION;
                    SELECT CAST(1 AS INT);
                END TRY
                BEGIN CATCH
                    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                    THROW;
                END CATCH;";

            return _client.ExecuteScalar<int>(query, parameters, CommandType.Text);
        }

        private bool RecordProductBatchUsage(
            int productId,
            int productBatchId,
            decimal quantity,
            MaterialBatchEntryType entryType,
            string? remark)
        {
            SqlParameter[] parameters =
            {
                new("@ProductId", productId),
                new("@ProductBatchId", productBatchId),
                new("@QuantityUsed", quantity),
                new("@EntryType", entryType.ToString()),
                new("@Remark", Utilities.DbValue(remark))
            };

            const string query = @"
                SET NOCOUNT ON;
                BEGIN TRY
                    BEGIN TRANSACTION;

                    UPDATE ProductBatchMaster
                    SET remainingQty = CASE
                            WHEN @EntryType = 'In' THEN ISNULL(remainingQty, 0) + @QuantityUsed
                            ELSE ISNULL(remainingQty, 0) - @QuantityUsed
                        END
                    WHERE id = @ProductBatchId
                      AND productId = @ProductId
                      AND isActive = 1
                      AND @QuantityUsed > 0
                      AND (@EntryType = 'In' OR ISNULL(remainingQty, 0) >= @QuantityUsed);

                    IF @@ROWCOUNT = 0
                    BEGIN
                        ROLLBACK TRANSACTION;
                        SELECT CAST(0 AS INT);
                        RETURN;
                    END;

                    INSERT INTO ProductBatchUsage
                        (remark, productBatchId, quantityUsed, productId, entryType)
                    VALUES
                        (@Remark, @ProductBatchId, @QuantityUsed, @ProductId, @EntryType);

                    COMMIT TRANSACTION;
                    SELECT CAST(1 AS INT);
                END TRY
                BEGIN CATCH
                    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                    THROW;
                END CATCH;";

            return _client.ExecuteScalar<int>(query, parameters, CommandType.Text) == 1;
        }

        public PageResult<ProductBatchListItem> GetProductBatchList(GetProductBatchListReq req)
        {
            string productId = EncryptionHelper.Decrypt(req.ProductEdid);

            if (string.IsNullOrWhiteSpace(productId))
                return new PageResult<ProductBatchListItem>();

            int pageNumber = req.Pagination.PageNumber > 0 ? req.Pagination.PageNumber : 1;
            int pageSize = req.Pagination.PageSize > 0 ? req.Pagination.PageSize : 10;
            int offset = (pageNumber - 1) * pageSize;

            SqlParameter[] p = new SqlParameter[4];
            p[0] = new SqlParameter("@ProductId", productId);
            p[1] = new SqlParameter("@Search", Utilities.DbValue(req.Search));
            p[2] = new SqlParameter("@Offset", offset);
            p[3] = new SqlParameter("@PageSize", pageSize);

            string query = @"SELECT pb.id, pb.batchNumber, pb.productionQty, pb.remainingQty, pb.isActive, pb.remarks, pb.createdAt,
                            pb.packedDate, pb.expiryDate,
                            pr.requestNumber AS productionNumber,
                            pr.id AS productionId,
                            ISNULL(u.entriesCount, 0) AS entriesCount,
                            ISNULL(u.totalUsed, 0) AS totalUsed,
                            u.latestUsageDate,
                            COUNT(*) OVER() AS totalCount
                     FROM ProductBatchMaster pb
                     OUTER APPLY (
                         SELECT TOP 1 pr.requestNumber, pr.id
                         FROM ProductionRequest pr
                         WHERE (pb.remarks LIKE '%' + pr.requestNumber + '%' OR pr.productProductionBatchId = pb.id)
                         ORDER BY pr.id DESC
                     ) pr
                     OUTER APPLY (
                         SELECT 
                             COUNT(1) AS entriesCount,
                             SUM(CASE WHEN pbu.entryType = 'Out' OR pbu.entryType IS NULL THEN quantityUsed ELSE 0 END) AS totalUsed,
                             MAX(createdAt) AS latestUsageDate
                         FROM ProductBatchUsage pbu
                         WHERE pbu.productBatchId = pb.id
                     ) u
                     WHERE pb.productId = @ProductId
                     AND (@Search IS NULL OR pb.batchNumber LIKE '%' + @Search + '%' OR pr.requestNumber LIKE '%' + @Search + '%' OR pb.remarks LIKE '%' + @Search + '%')
                     ORDER BY pb.id DESC
                     OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            PageResult<ProductBatchListItem> res = new PageResult<ProductBatchListItem>
            {
                TotalCount = dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["totalCount"]) : 0,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            foreach (DataRow row in dt.Rows)
            {
                string? remarks = row.Field<string?>("remarks");
                string? prodNum = row.IsNull("productionNumber") ? null : row.Field<string>("productionNumber");
                if (string.IsNullOrWhiteSpace(prodNum) && !string.IsNullOrWhiteSpace(remarks))
                {
                    var m = System.Text.RegularExpressions.Regex.Match(remarks, @"PRD-\d+");
                    if (m.Success) prodNum = m.Value;
                }

                res.Items.Add(new ProductBatchListItem
                {
                    ProductBatchEdid = EncryptionHelper.Encrypt(Convert.ToString(row["id"])),
                    BatchNumber = row.Field<string>("batchNumber") ?? string.Empty,
                    ProductionNumber = prodNum,
                    ProductionEdid = row.IsNull("productionId") ? null : EncryptionHelper.Encrypt(Convert.ToString(row["productionId"])),
                    ProductionQty = row.Field<decimal>("productionQty"),
                    RemainingQty = row.Field<decimal>("remainingQty"),
                    IsActive = row.Field<bool>("isActive"),
                    Remarks = remarks,
                    CreatedAt = row.Field<DateTime>("createdAt"),
                    PackedDate = row.IsNull("packedDate") ? (DateTime?)null : row.Field<DateTime>("packedDate"),
                    ExpiryDate = row.IsNull("expiryDate") ? (DateTime?)null : row.Field<DateTime>("expiryDate"),
                    EntriesCount = Convert.ToInt32(row["entriesCount"]),
                    TotalUsed = Convert.ToDecimal(row["totalUsed"]),
                    LatestUsageDate = row.IsNull("latestUsageDate") ? (DateTime?)null : row.Field<DateTime>("latestUsageDate")
                });
            }

            return res;
        }
    }
}