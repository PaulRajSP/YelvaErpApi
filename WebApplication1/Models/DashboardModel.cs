using HoneyErpAPI.Common;
using System;
using System.Collections.Generic;

namespace HoneyErpAPI.Models
{
    public class DashboardTodaySummaryRes : BaseApiResponse
    {
        public string Period { get; set; } = "today";

        // Metrics for the selected period (today / 1week / 1month)
        public int PeriodOrders { get; set; }
        public decimal PeriodSales { get; set; }
        public int Pending { get; set; }
        public int Processing { get; set; }
        public int Shipped { get; set; }
        public int Delivered { get; set; }
        public int Cancelled { get; set; }

        // Backwards compatibility properties
        public int TodaysOrders { get => PeriodOrders; set => PeriodOrders = value; }
        public decimal TodaysSales { get => PeriodSales; set => PeriodSales = value; }
        public int TodaysPending { get => Pending; set => Pending = value; }
        public int TodaysProcessing { get => Processing; set => Processing = value; }
        public int TodaysShipped { get => Shipped; set => Shipped = value; }
        public int TodaysDelivered { get => Delivered; set => Delivered = value; }
        public int TodaysCancelled { get => Cancelled; set => Cancelled = value; }

        // All-time metrics for reference
        public int TotalOrders { get; set; }
        public decimal TotalSales { get; set; }
        public int AllPending { get; set; }
        public int AllProcessing { get; set; }
        public int AllShipped { get; set; }
        public int AllDelivered { get; set; }
    }

    public class SalesReportRes : BaseApiResponse
    {
        public string Period { get; set; } = "1month";
        public decimal TotalRevenue { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal ProfitMargin { get; set; }
        public int TotalOrders { get; set; }
        public int TodaysOrders { get; set; }
        public decimal TodaysRevenue { get; set; }

        public List<ProductSalesPerformanceItem> ProductSales { get; set; } = new List<ProductSalesPerformanceItem>();
        public List<PackingHistoryItem> PackingHistory { get; set; } = new List<PackingHistoryItem>();
    }

    public class ProductSalesPerformanceItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int QtySold { get; set; }
        public decimal Revenue { get; set; }
        public decimal Cogs { get; set; }
        public decimal Profit { get; set; }
    }

    public class PackingHistoryItem
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int ItemsCount { get; set; }
        public decimal Revenue { get; set; }
        public decimal TotalAmount { get => Revenue; set => Revenue = value; }
        public decimal DeliveryCharge { get; set; }
        public decimal Cogs { get; set; }
        public decimal TotalCost { get; set; }
        public decimal Profit { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? HandedOverAt { get; set; }
        public string? CourierName { get; set; }
    }

    public class PackingHistoryRes : BaseApiResponse
    {
        public string Period { get; set; } = "1month";
        public List<PackingHistoryItem> Items { get; set; } = new List<PackingHistoryItem>();
    }
}
