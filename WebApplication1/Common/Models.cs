using HoneyErpAPI.Common;

namespace WebApplication1.Common
{
    public class Models
    {

        public class PaginationRequest
        {
            public int PageNumber { get; set; } = 1;
            public int PageSize { get; set; } = 10;
        }

        public class PageResult<T> : BaseApiResponse
        {
            public List<T> Items { get; set; } = new List<T>();
            public int TotalCount { get; set; }
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
            public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
        }


        public class ProductPriceItem
        {
            public string PriceItemEdid { get; set; }
            public string CustomerType { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public decimal Mrp { get; set; }
        }

        public class CreateRes
        {
            public int Did { get; set; }
            public string Edid { get; set; }
        }

        public class MaterialBatchUsage
        {
            public string? Remark { get; set; }
            public string MaterialBatchEdid { get; set; }
            public decimal QuantityUsed { get; set; }
            public string MaterialEdid { get; set; }
        }

        public class ProductBatchUsage
        {
            public string? Remark { get; set; }
            public string ProductBatchEdid { get; set; } = string.Empty;
            public decimal QuantityUsed { get; set; }
            public string ProductEdid { get; set; } = string.Empty;
        }
    }
}
