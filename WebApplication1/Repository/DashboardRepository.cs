using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace HoneyErpAPI.Repository
{
    public interface IDashboardRepository
    {
        DashboardTodaySummaryRes GetSummary(string? period, string? dateString, int? timezoneOffsetMinutes);
        SalesReportRes GetSalesReport(string? period, string? dateString, int? timezoneOffsetMinutes);
        TopSellingProductsRes GetTopSellingProducts(string? period, string? dateString, int? timezoneOffsetMinutes);
    }

    public class DashboardRepository : IDashboardRepository
    {
        private readonly DbClient _client;

        public DashboardRepository(DbClient client)
        {
            _client = client;
        }

        private const string CostCte = @"
            WITH MaterialBatchRates AS (
                -- Rate per material batch from actual purchases
                SELECT 
                    pi.materialBatchId,
                    pi.materialId,
                    SUM(pi.quantity * pi.rate) / NULLIF(SUM(pi.quantity), 0) AS batchRate
                FROM PurchaseItem pi
                WHERE pi.materialBatchId IS NOT NULL
                GROUP BY pi.materialBatchId, pi.materialId
            ),
            MaterialAvgRates AS (
                -- Overall average purchase rate per material
                SELECT 
                    pi.materialId,
                    SUM(pi.quantity * pi.rate) / NULLIF(SUM(pi.quantity), 0) AS avgRate
                FROM PurchaseItem pi
                GROUP BY pi.materialId
            ),
            RecipeCost AS (
                -- Standard formula cost from RecipeItem and material purchase rates
                SELECT 
                    ri.productId,
                    SUM(ri.qty * COALESCE(mar.avgRate, 0)) AS unitCost
                FROM RecipeItem ri
                LEFT JOIN MaterialAvgRates mar ON ri.materialId = mar.materialId
                WHERE (ri.isActive = 1 OR ri.isActive IS NULL)
                GROUP BY ri.productId
            ),
            FallbackMaterialRate AS (
                SELECT ISNULL(AVG(avgRate), 0.25) AS defaultRate
                FROM MaterialAvgRates
            ),
            SingleProductCost AS (
                -- 1. Recipe cost
                -- 2. Product qty * default material rate
                SELECT 
                    pm.id AS productId,
                    CASE 
                        WHEN ISNULL(rc.unitCost, 0) > 0 THEN rc.unitCost
                        WHEN pm.qty IS NOT NULL AND pm.qty > 0 THEN pm.qty * fmr.defaultRate
                        ELSE 0
                    END AS unitCost
                FROM ProductMaster pm
                CROSS JOIN FallbackMaterialRate fmr
                LEFT JOIN RecipeCost rc ON pm.id = rc.productId
                WHERE pm.isCombo = 0
            ),
            ComboCost AS (
                -- Sum of constituent single products
                SELECT 
                    pci.comboProductId AS productId,
                    SUM(pci.quantity * ISNULL(spc.unitCost, 0)) AS unitCost
                FROM ProductComboItem pci
                LEFT JOIN SingleProductCost spc ON pci.productId = spc.productId
                GROUP BY pci.comboProductId
            ),
            AllProductCost AS (
                SELECT 
                    pm.id AS productId,
                    CASE 
                        WHEN pm.isCombo = 1 THEN ISNULL(cc.unitCost, 0)
                        ELSE ISNULL(spc.unitCost, 0)
                    END AS unitCost
                FROM ProductMaster pm
                LEFT JOIN SingleProductCost spc ON pm.id = spc.productId
                LEFT JOIN ComboCost cc ON pm.id = cc.productId
            )";

        public DashboardTodaySummaryRes GetSummary(string? period, string? dateString, int? timezoneOffsetMinutes)
        {
            string normPeriod = (period ?? "today").Trim().ToLowerInvariant();

            DateTime localToday;
            if (!string.IsNullOrWhiteSpace(dateString) && DateTime.TryParse(dateString, out DateTime parsedDate))
            {
                localToday = parsedDate.Date;
            }
            else
            {
                localToday = DateTime.UtcNow.Date;
            }

            int offset = timezoneOffsetMinutes ?? 0;
            DateTime endUtc = DateTime.SpecifyKind(localToday.AddDays(1).AddMinutes(offset), DateTimeKind.Utc);

            DateTime startUtc;
            switch (normPeriod)
            {
                case "1week":
                case "week":
                case "7days":
                    startUtc = DateTime.SpecifyKind(localToday.AddDays(-6).AddMinutes(offset), DateTimeKind.Utc);
                    break;

                case "1month":
                case "month":
                case "30days":
                    startUtc = DateTime.SpecifyKind(localToday.AddDays(-29).AddMinutes(offset), DateTimeKind.Utc);
                    break;

                case "today":
                default:
                    normPeriod = "today";
                    startUtc = DateTime.SpecifyKind(localToday.AddMinutes(offset), DateTimeKind.Utc);
                    break;
            }

            SqlParameter[] p =
            {
                new SqlParameter("@StartUtc", startUtc),
                new SqlParameter("@EndUtc", endUtc)
            };

            string query = @"
                SELECT 
                    COUNT(CASE WHEN o.createdAt >= @StartUtc AND o.createdAt < @EndUtc THEN 1 END) AS PeriodOrders,
                    ISNULL(SUM(CASE WHEN o.createdAt >= @StartUtc AND o.createdAt < @EndUtc AND o.status != 'Cancelled' THEN o.totalAmount ELSE 0 END), 0) AS PeriodSales,
                    COUNT(CASE WHEN o.createdAt >= @StartUtc AND o.createdAt < @EndUtc AND o.status IN ('OrderPlaced', 'Confirmed') THEN 1 END) AS Pending,
                    COUNT(CASE WHEN o.createdAt >= @StartUtc AND o.createdAt < @EndUtc AND o.status IN ('Packing', 'ReadyToShip') THEN 1 END) AS Processing,
                    COUNT(CASE WHEN o.createdAt >= @StartUtc AND o.createdAt < @EndUtc AND o.status IN ('Shipped', 'OutForDelivery') THEN 1 END) AS Shipped,
                    COUNT(CASE WHEN o.createdAt >= @StartUtc AND o.createdAt < @EndUtc AND o.status = 'Delivered' THEN 1 END) AS Delivered,
                    COUNT(CASE WHEN o.createdAt >= @StartUtc AND o.createdAt < @EndUtc AND o.status = 'Cancelled' THEN 1 END) AS Cancelled,

                    COUNT(*) AS TotalOrders,
                    ISNULL(SUM(CASE WHEN o.status != 'Cancelled' THEN o.totalAmount ELSE 0 END), 0) AS TotalSales,
                    COUNT(CASE WHEN o.status IN ('OrderPlaced', 'Confirmed') THEN 1 END) AS AllPending,
                    COUNT(CASE WHEN o.status IN ('Packing', 'ReadyToShip') THEN 1 END) AS AllProcessing,
                    COUNT(CASE WHEN o.status IN ('Shipped', 'OutForDelivery') THEN 1 END) AS AllShipped,
                    COUNT(CASE WHEN o.status = 'Delivered' THEN 1 END) AS AllDelivered
                FROM OrderMaster o";

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];
            DashboardTodaySummaryRes res = new DashboardTodaySummaryRes { Period = normPeriod };

            if (dt.Rows.Count > 0)
            {
                DataRow r = dt.Rows[0];
                res.PeriodOrders = r.Field<int>("PeriodOrders");
                res.PeriodSales  = r.Field<decimal>("PeriodSales");
                res.Pending      = r.Field<int>("Pending");
                res.Processing   = r.Field<int>("Processing");
                res.Shipped      = r.Field<int>("Shipped");
                res.Delivered    = r.Field<int>("Delivered");
                res.Cancelled    = r.Field<int>("Cancelled");

                res.TotalOrders   = r.Field<int>("TotalOrders");
                res.TotalSales    = r.Field<decimal>("TotalSales");
                res.AllPending    = r.Field<int>("AllPending");
                res.AllProcessing = r.Field<int>("AllProcessing");
                res.AllShipped    = r.Field<int>("AllShipped");
                res.AllDelivered  = r.Field<int>("AllDelivered");
            }

            return res;
        }

        public SalesReportRes GetSalesReport(string? period, string? dateString, int? timezoneOffsetMinutes)
        {
            string normPeriod = (period ?? "1month").Trim().ToLowerInvariant();

            DateTime localToday;
            if (!string.IsNullOrWhiteSpace(dateString) && DateTime.TryParse(dateString, out DateTime parsedDate))
            {
                localToday = parsedDate.Date;
            }
            else
            {
                localToday = DateTime.UtcNow.Date;
            }

            int offset = timezoneOffsetMinutes ?? 0;
            DateTime endUtc = DateTime.SpecifyKind(localToday.AddDays(1).AddMinutes(offset), DateTimeKind.Utc);
            DateTime todayStartUtc = DateTime.SpecifyKind(localToday.AddMinutes(offset), DateTimeKind.Utc);

            DateTime startUtc;
            switch (normPeriod)
            {
                case "today":
                    startUtc = todayStartUtc;
                    break;

                case "1week":
                case "week":
                case "7days":
                    startUtc = DateTime.SpecifyKind(localToday.AddDays(-6).AddMinutes(offset), DateTimeKind.Utc);
                    break;

                case "all":
                    startUtc = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    break;

                case "1month":
                case "month":
                case "30days":
                default:
                    normPeriod = "1month";
                    startUtc = DateTime.SpecifyKind(localToday.AddDays(-29).AddMinutes(offset), DateTimeKind.Utc);
                    break;
            }

            SqlParameter[] p =
            {
                new SqlParameter("@StartUtc", startUtc),
                new SqlParameter("@EndUtc", endUtc),
                new SqlParameter("@TodayStartUtc", todayStartUtc)
            };

            // 1. Aggregated Summary
            string summaryQuery = @"
                SELECT 
                    COUNT(CASE WHEN o.createdAt >= @StartUtc AND o.createdAt < @EndUtc AND o.status != 'Cancelled' THEN 1 END) AS PeriodOrders,
                    ISNULL(SUM(CASE WHEN o.createdAt >= @StartUtc AND o.createdAt < @EndUtc AND o.status != 'Cancelled' THEN o.totalAmount ELSE 0 END), 0) AS PeriodRevenue,
                    ISNULL(SUM(CASE WHEN o.createdAt >= @StartUtc AND o.createdAt < @EndUtc AND o.status != 'Cancelled' THEN ISNULL(o.deliveryChargeGiven, 0) ELSE 0 END), 0) AS PeriodDelivery,
                    COUNT(CASE WHEN o.createdAt >= @TodayStartUtc AND o.createdAt < @EndUtc AND o.status != 'Cancelled' THEN 1 END) AS TodaysOrders,
                    ISNULL(SUM(CASE WHEN o.createdAt >= @TodayStartUtc AND o.createdAt < @EndUtc AND o.status != 'Cancelled' THEN o.totalAmount ELSE 0 END), 0) AS TodaysRevenue
                FROM OrderMaster o";

            DataTable summaryDt = _client.ExecuteAdapter(summaryQuery, p, CommandType.Text).Tables[0];
            SalesReportRes res = new SalesReportRes { Period = normPeriod };
            decimal periodDelivery = 0;

            if (summaryDt.Rows.Count > 0)
            {
                DataRow r = summaryDt.Rows[0];
                res.TotalOrders   = r.Field<int>("PeriodOrders");
                res.TotalRevenue  = r.Field<decimal>("PeriodRevenue");
                periodDelivery    = r.Field<decimal>("PeriodDelivery");
                res.TodaysOrders  = r.Field<int>("TodaysOrders");
                res.TodaysRevenue = r.Field<decimal>("TodaysRevenue");
            }

            // 2. Product Sales Performance with recursive Recipe + Combo COGS
            string productQuery = CostCte + @"
                SELECT 
                    oi.productId,
                    oi.productNameSnapshot,
                    SUM(oi.quantity) AS QtySold,
                    SUM(oi.quantity * oi.unitPrice) AS Revenue,
                    ISNULL(apc.unitCost, 0) AS UnitCost
                FROM OrderItem oi
                INNER JOIN OrderMaster o ON oi.orderId = o.id
                LEFT JOIN AllProductCost apc ON oi.productId = apc.productId
                WHERE o.status != 'Cancelled'
                  AND o.createdAt >= @StartUtc AND o.createdAt < @EndUtc
                GROUP BY oi.productId, oi.productNameSnapshot, apc.unitCost
                ORDER BY QtySold DESC";

            SqlParameter[] prodP =
            {
                new SqlParameter("@StartUtc", startUtc),
                new SqlParameter("@EndUtc", endUtc)
            };

            DataTable prodDt = _client.ExecuteAdapter(productQuery, prodP, CommandType.Text).Tables[0];
            decimal totalCogs = 0;

            foreach (DataRow row in prodDt.Rows)
            {
                int pId = row.Field<int>("productId");
                string pName = row.Field<string>("productNameSnapshot");
                int qty = Convert.ToInt32(row["QtySold"]);
                decimal rev = Convert.ToDecimal(row["Revenue"]);
                decimal unitCost = Convert.ToDecimal(row["UnitCost"]);

                // If unit cost is missing or not configured in recipes, estimate at 35% of unit price
                if (unitCost <= 0)
                {
                    unitCost = Math.Round((rev / Math.Max(1, qty)) * 0.35m, 2);
                }

                decimal cogs = Math.Round(unitCost * qty, 2);
                decimal profit = rev - cogs;
                totalCogs += cogs;

                res.ProductSales.Add(new ProductSalesPerformanceItem
                {
                    ProductId   = pId,
                    ProductName = pName,
                    QtySold     = qty,
                    Revenue     = rev,
                    Cogs        = cogs,
                    Profit      = profit
                });
            }

            // Total Cost = COGS + Delivery Charge; Profit = Revenue - Total Cost
            res.GrossProfit = res.TotalRevenue - totalCogs - periodDelivery;
            if (res.GrossProfit < 0) res.GrossProfit = 0;
            res.ProfitMargin = res.TotalRevenue > 0
                ? Math.Round((res.GrossProfit / res.TotalRevenue) * 100m, 1)
                : 0;

            return res;
        }

        public TopSellingProductsRes GetTopSellingProducts(string? period, string? dateString, int? timezoneOffsetMinutes)
        {
            string normPeriod = (period ?? "today").Trim().ToLowerInvariant();

            DateTime localToday;
            if (!string.IsNullOrWhiteSpace(dateString) && DateTime.TryParse(dateString, out DateTime parsedDate))
            {
                localToday = parsedDate.Date;
            }
            else
            {
                localToday = DateTime.UtcNow.Date;
            }

            int offset = timezoneOffsetMinutes ?? 0;
            DateTime endUtc = DateTime.SpecifyKind(localToday.AddDays(1).AddMinutes(offset), DateTimeKind.Utc);

            DateTime startUtc;
            switch (normPeriod)
            {
                case "1week":
                case "week":
                case "7days":
                    startUtc = DateTime.SpecifyKind(localToday.AddDays(-6).AddMinutes(offset), DateTimeKind.Utc);
                    break;

                case "1month":
                case "month":
                case "30days":
                    normPeriod = "1month";
                    startUtc = DateTime.SpecifyKind(localToday.AddDays(-29).AddMinutes(offset), DateTimeKind.Utc);
                    break;

                case "today":
                default:
                    normPeriod = "today";
                    startUtc = DateTime.SpecifyKind(localToday.AddMinutes(offset), DateTimeKind.Utc);
                    break;
            }

            const string query = @"
                SELECT TOP (3)
                    oi.productId AS ProductId,
                    COALESCE(NULLIF(MAX(oi.productNameSnapshot), ''), CONCAT('Product ', oi.productId)) AS ProductName,
                    SUM(oi.quantity) AS QtySold,
                    SUM(oi.quantity * oi.unitPrice) AS Revenue
                FROM OrderItem oi
                INNER JOIN OrderMaster o ON oi.orderId = o.id
                WHERE o.status != 'Cancelled'
                  AND o.createdAt >= @StartUtc AND o.createdAt < @EndUtc
                GROUP BY oi.productId
                ORDER BY SUM(oi.quantity * oi.unitPrice) DESC, SUM(oi.quantity) DESC, oi.productId ASC";

            SqlParameter[] parameters =
            {
                new SqlParameter("@StartUtc", startUtc),
                new SqlParameter("@EndUtc", endUtc)
            };

            DataTable dt = _client.ExecuteAdapter(query, parameters, CommandType.Text).Tables[0];
            TopSellingProductsRes response = new TopSellingProductsRes { Period = normPeriod };

            foreach (DataRow row in dt.Rows)
            {
                response.Items.Add(new TopSellingProductItem
                {
                    ProductId = Convert.ToInt32(row["ProductId"]),
                    ProductName = Convert.ToString(row["ProductName"]) ?? string.Empty,
                    QtySold = Convert.ToInt32(row["QtySold"]),
                    Revenue = Convert.ToDecimal(row["Revenue"])
                });
            }

            return response;
        }
    }
}
