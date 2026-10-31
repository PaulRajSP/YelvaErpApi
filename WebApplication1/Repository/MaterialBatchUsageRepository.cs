using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Repository
{
    public interface IMaterialBatchUsageRepository
    {
        MaterialUsageHistoryRes GetUsageHistory(GetMaterialUsageHistoryReq req);
        PageResult<GetMaterialBatchListItem> GetMaterialBatchList(GetMaterialBatchListReq req);
        bool Add(MaterialBatchUsage req);
        bool Add(int materialId, int materialBatchId, decimal quantity, MaterialBatchEntryType entryType, string? remark, bool updateQtyPurchased = false);
        bool AdjustBatchQuantity(int materialBatchId, MaterialBatchAdjustmentReq req);
        int CloseBatch(int materialBatchId);
        bool AdjustLatestMaterialStock(int materialId, decimal quantityChange);
    }

    public class MaterialBatchUsageRepository : IMaterialBatchUsageRepository
    {
        private readonly DbClient _client;

        public MaterialBatchUsageRepository(DbClient client) => _client = client;

        public MaterialUsageHistoryRes GetUsageHistory(GetMaterialUsageHistoryReq req)
        {
            string materialId = EncryptionHelper.Decrypt(req.MaterialEdid);
            string materialBatchId = EncryptionHelper.Decrypt(req.MaterialBatchEdid);
            MaterialUsageHistoryRes res = new() { Items = new List<MaterialUsageHistoryItem>() };

            if (string.IsNullOrWhiteSpace(materialId) || string.IsNullOrWhiteSpace(materialBatchId))
                return res;

            SqlParameter[] parameters =
            {
                new("@MaterialId", materialId),
                new("@MaterialBatchId", materialBatchId)
            };
            const string query = @"
                SELECT remark, quantityUsed, createdAt, entryType
                FROM MaterialBatchUsage
                WHERE materialId = @MaterialId
                  AND materialBatchId = @MaterialBatchId
                ORDER BY createdAt DESC";

            DataTable rows = _client.ExecuteAdapter(query, parameters, CommandType.Text).Tables[0];
            foreach (DataRow row in rows.Rows)
            {
                res.Items.Add(new MaterialUsageHistoryItem
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

        public PageResult<GetMaterialBatchListItem> GetMaterialBatchList(GetMaterialBatchListReq req)
        {
            string materialId = EncryptionHelper.Decrypt(req.materialEdid);
            if (string.IsNullOrWhiteSpace(materialId))
                return new PageResult<GetMaterialBatchListItem>();

            int pageNumber = req.Pagination.PageNumber > 0 ? req.Pagination.PageNumber : 1;
            int pageSize = req.Pagination.PageSize > 0 ? req.Pagination.PageSize : 10;
            int offset = (pageNumber - 1) * pageSize;
            SqlParameter[] parameters =
            {
                new("@MaterialId", materialId),
                new("@Search", Utilities.DbValue(req.Search)),
                new("@Offset", offset),
                new("@PageSize", pageSize)
            };

            const string query = @"
                SELECT mb.id, mb.batchNumber, mb.qtyRemaining, mb.isActive, COUNT(*) OVER() AS totalCount,
                       usageSummary.latestUsageDate, usageSummary.entriesCount, usageSummary.totalUsed
                FROM MaterialBatchMaster mb
                OUTER APPLY
                (
                    SELECT MAX(mbu.createdAt) AS latestUsageDate,
                           COUNT(*) AS entriesCount,
                           COALESCE(SUM(CASE
                               WHEN mbu.entryType = 'Out' OR mbu.entryType IS NULL THEN mbu.quantityUsed
                               ELSE 0
                           END), 0) AS totalUsed
                    FROM MaterialBatchUsage mbu
                    WHERE mbu.materialBatchId = mb.id
                ) usageSummary
                WHERE mb.materialId = @MaterialId
                  AND (@Search IS NULL OR mb.batchNumber LIKE '%' + @Search + '%')
                ORDER BY mb.id DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            DataTable rows = _client.ExecuteAdapter(query, parameters, CommandType.Text).Tables[0];
            PageResult<GetMaterialBatchListItem> res = new()
            {
                TotalCount = rows.Rows.Count > 0 ? Convert.ToInt32(rows.Rows[0]["totalCount"]) : 0,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            foreach (DataRow row in rows.Rows)
            {
                res.Items.Add(new GetMaterialBatchListItem
                {
                    materialBatchEdid = EncryptionHelper.Encrypt(Convert.ToString(row["id"])),
                    batchNumber = row.Field<string>("batchNumber") ?? string.Empty,
                    qtyRemaining = row.Field<decimal>("qtyRemaining"),
                    isActive = !row.IsNull("isActive") && row.Field<bool>("isActive"),
                    latestUsageDate = row.Field<DateTime?>("latestUsageDate"),
                    entriesCount = row.Field<int>("entriesCount"),
                    totalUsed = row.Field<decimal>("totalUsed")
                });
            }

            return res;
        }

        public bool Add(
            MaterialBatchUsage req)
        {
            return Add(
                EncryptionHelper.Decrypt<int>(req.MaterialEdid),
                EncryptionHelper.Decrypt<int>(req.MaterialBatchEdid),
                req.QuantityUsed,
                req.EntryType,
                req.Remark);
        }

        public bool Add(
            int materialId,
            int materialBatchId,
            decimal quantity,
            MaterialBatchEntryType entryType,
            string? remark,
            bool updateQtyPurchased = false)
        {
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), "Material batch quantity must be greater than zero.");

            SqlParameter[] parameters =
            {
                new("@MaterialId", materialId),
                new("@MaterialBatchId", materialBatchId),
                new("@Quantity", quantity),
                new("@EntryType", entryType.ToString()),
                new("@Remark", Utilities.DbValue(remark)),
                new("@UpdateQtyPurchased", updateQtyPurchased)
            };

            const string query = @"
                SET NOCOUNT ON;
                BEGIN TRY
                    BEGIN TRANSACTION;

                    UPDATE MaterialBatchMaster
                    SET qtyRemaining = CASE
                            WHEN @EntryType = 'In' THEN ISNULL(qtyRemaining, 0) + @Quantity
                            ELSE ISNULL(qtyRemaining, 0) - @Quantity
                        END,
                        qtyPurchased = CASE
                            WHEN @UpdateQtyPurchased = 1 AND @EntryType = 'In'
                                THEN ISNULL(qtyPurchased, 0) + @Quantity
                            ELSE ISNULL(qtyPurchased, 0)
                        END
                    WHERE id = @MaterialBatchId
                      AND materialId = @MaterialId
                      AND (isActive = 1 OR isActive IS NULL)
                      AND @Quantity > 0
                      AND (@EntryType = 'In' OR ISNULL(qtyRemaining, 0) >= @Quantity);

                    IF @@ROWCOUNT = 0
                    BEGIN
                        ROLLBACK TRANSACTION;
                        SELECT CAST(0 AS INT);
                        RETURN;
                    END;

                    INSERT INTO MaterialBatchUsage
                        (remark, materialBatchId, quantityUsed, materialId, entryType)
                    VALUES
                        (@Remark, @MaterialBatchId, @Quantity, @MaterialId, @EntryType);

                    COMMIT TRANSACTION;
                    SELECT CAST(1 AS INT);
                END TRY
                BEGIN CATCH
                    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                    THROW;
                END CATCH;";

            return _client.ExecuteScalar<int>(query, parameters, CommandType.Text) == 1;
        }

        public bool AdjustBatchQuantity(int materialBatchId, MaterialBatchAdjustmentReq req)
        {
            SqlParameter[] parameters = { new("@MaterialBatchId", materialBatchId) };
            int materialId = _client.ExecuteScalar<int>(
                "SELECT materialId FROM MaterialBatchMaster WHERE id = @MaterialBatchId",
                parameters,
                CommandType.Text);
            if (materialId == 0) return false;

            string remark = req.Remark?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(remark))
                throw new ArgumentException("A remark is required for every stock adjustment.", nameof(req));

            return Add(materialId, materialBatchId, req.Quantity, req.EntryType, remark);
        }

        public int CloseBatch(int materialBatchId)
        {
            SqlParameter[] parameters = { new("@MaterialBatchId", materialBatchId) };
            const string query = @"
                SET NOCOUNT ON;
                BEGIN TRY
                    BEGIN TRANSACTION;

                    DECLARE @IsActive BIT;
                    DECLARE @QtyRemaining DECIMAL(18, 4);
                    DECLARE @Found BIT = 0;
                    SELECT @IsActive = isActive, @QtyRemaining = qtyRemaining
                    FROM MaterialBatchMaster WITH (UPDLOCK, HOLDLOCK)
                    WHERE id = @MaterialBatchId;
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

                    IF ISNULL(@QtyRemaining, 0) <> 0
                    BEGIN
                        ROLLBACK TRANSACTION;
                        SELECT CAST(-2 AS INT);
                        RETURN;
                    END;

                    UPDATE MaterialBatchMaster SET isActive = 0 WHERE id = @MaterialBatchId;
                    COMMIT TRANSACTION;
                    SELECT CAST(1 AS INT);
                END TRY
                BEGIN CATCH
                    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                    THROW;
                END CATCH;";

            return _client.ExecuteScalar<int>(query, parameters, CommandType.Text);
        }

        public bool AdjustLatestMaterialStock(int materialId, decimal quantityChange)
        {
            if (quantityChange == 0) return true;

            SqlParameter[] parameters =
            {
                new("@MaterialId", materialId),
                new("@Quantity", Math.Abs(quantityChange)),
                new("@EntryType", quantityChange > 0 ? MaterialBatchEntryType.In.ToString() : MaterialBatchEntryType.Out.ToString()),
                new("@Remark", "Manual stock adjustment")
            };

            const string query = @"
                SET NOCOUNT ON;
                BEGIN TRY
                    BEGIN TRANSACTION;

                    DECLARE @MaterialBatchId INT;
                    SELECT TOP 1 @MaterialBatchId = id
                    FROM MaterialBatchMaster WITH (UPDLOCK, HOLDLOCK)
                    WHERE materialId = @MaterialId AND isActive = 1
                    ORDER BY id DESC;

                    IF @MaterialBatchId IS NULL
                    BEGIN
                        IF @EntryType = 'Out'
                        BEGIN
                            ROLLBACK TRANSACTION;
                            SELECT CAST(0 AS INT);
                            RETURN;
                        END;

                        INSERT INTO MaterialBatchMaster (materialId, batchNumber, qtyPurchased, qtyRemaining, isActive)
                        VALUES (@MaterialId, 'DEFAULT', 0, 0, 1);
                        SET @MaterialBatchId = SCOPE_IDENTITY();
                    END;

                    UPDATE MaterialBatchMaster
                    SET qtyRemaining = CASE
                            WHEN @EntryType = 'In' THEN ISNULL(qtyRemaining, 0) + @Quantity
                            ELSE ISNULL(qtyRemaining, 0) - @Quantity
                        END,
                        qtyPurchased = CASE
                            WHEN @EntryType = 'In' THEN ISNULL(qtyPurchased, 0) + @Quantity
                            ELSE ISNULL(qtyPurchased, 0)
                        END
                    WHERE id = @MaterialBatchId
                      AND (@EntryType = 'In' OR ISNULL(qtyRemaining, 0) >= @Quantity);

                    IF @@ROWCOUNT = 0
                    BEGIN
                        ROLLBACK TRANSACTION;
                        SELECT CAST(0 AS INT);
                        RETURN;
                    END;

                    INSERT INTO MaterialBatchUsage
                        (remark, materialBatchId, quantityUsed, materialId, entryType)
                    VALUES
                        (@Remark, @MaterialBatchId, @Quantity, @MaterialId, @EntryType);

                    COMMIT TRANSACTION;
                    SELECT CAST(1 AS INT);
                END TRY
                BEGIN CATCH
                    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                    THROW;
                END CATCH;";

            return _client.ExecuteScalar<int>(query, parameters, CommandType.Text) == 1;
        }
    }
}
