using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Services;
using Microsoft.AspNetCore.Mvc;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductController : ControllerBase
    {
        private readonly ProductService _service;
        private readonly ILogger<ProductController> _logger;

        public ProductController(ProductService service, ILogger<ProductController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        [HttpPost("list")]
        public IActionResult GetAllProduct([FromBody] GetProductListReq req)
        {
            ApiResponseModel<PageResult<ProductListItem>> res = new ApiResponseModel<PageResult<ProductListItem>>();
            res.Success = true;
            res.Data = _service.GetAllProduct(req);
            res.Message = "Products fetched successfully";
            return Ok(res);
        }

        [HttpGet("{productEdid}")]
        public IActionResult GetById(string productEdid)
        {
            ApiResponseModel<GetProductByIdRes> res = new ApiResponseModel<GetProductByIdRes>();
            res.Success = true;
            res.Data= _service.GetById(productEdid);
            res.Message = "Product fetched successfully";
            return Ok(res);
        }

        [HttpPost]
        public IActionResult AddProduct([FromBody] AddProductReq req)
        {
            ApiResponseModel<AddProductRes> res = new ApiResponseModel<AddProductRes>();
            res.Success = true;
            res.Data = _service.AddProduct(req);
            res.Message = "Product added successfully";
            return Ok(res);
        }

        [HttpPost("{productEdid}/status")]
        public IActionResult ToggleActive(string productEdid, [FromBody] ToggleActiveReq req)
        {
            ApiResponseModel<ToggleActiveRes> res = new ApiResponseModel<ToggleActiveRes>();
            res.Success = true;
            res.Data = _service.ToggleActive(productEdid, req);
            res.Message = req.Active ? "Product activated successfully" : "Product deactivated successfully";
            return Ok(res);
        }

        [HttpGet("{productEdid}/combo-items")]
        public IActionResult GetComboItems(string productEdid)
        {
            ApiResponseModel<GetComboItemsRes> res = new ApiResponseModel<GetComboItemsRes>();
            res.Success = true;
            res.Data    = _service.GetComboItems(productEdid);
            res.Message = "Combo items fetched successfully";
            return Ok(res);
        }

        [HttpPost("{productEdid}/used-in-combos")]
        public IActionResult GetCombosUsingProduct(string productEdid, [FromBody] GetCombosUsingProductReq req)
        {
            ApiResponseModel<GetCombosUsingProductRes> res = new ApiResponseModel<GetCombosUsingProductRes>();
            res.Success = true;
            res.Data    = _service.GetCombosUsingProduct(productEdid, req);
            res.Message = "Fetched combos successfully";
            return Ok(res);
        }

        [HttpPost("{productEdid}/combo-items/save")]
        public IActionResult SaveComboItems(string productEdid, [FromBody] SaveComboItemsReq req)
        {
            ApiResponseModel<SaveComboItemsRes> res = new ApiResponseModel<SaveComboItemsRes>();
            _service.SaveComboItems(productEdid, req);
            res.Success = true;
            res.Message = "Combo items saved successfully";
            return Ok(res);
        }

        [HttpGet("{productEdid}/pricing")]
        public IActionResult GetPricing(string productEdid, [FromQuery] CustomerType? customerType = null)
        {
            ApiResponseModel<GetPricingRes> res = new ApiResponseModel<GetPricingRes>();
            res.Success = true;
            res.Data    = _service.GetPricing(productEdid, customerType);
            res.Message = "Pricing fetched successfully";
            return Ok(res);
        }

        [HttpPost("{productEdid}/pricing/save")]
        public IActionResult SavePricing(string productEdid, [FromBody] SavePricingReq req)
        {
            ApiResponseModel<SavePricingRes> res = new ApiResponseModel<SavePricingRes>();
            _service.SavePricing(productEdid, req);
            res.Success = true;
            res.Message = "Pricing saved successfully";
            return Ok(res);
        }

        [HttpPost("{productEdid}/price-history")]
        public IActionResult GetPriceHistory(string productEdid, [FromBody] GetPriceHistoryReq req)
        {
            req.ProductEdid = productEdid; // Ensure the ID from the route is used
            ApiResponseModel<GetPriceHistoryRes> res = new ApiResponseModel<GetPriceHistoryRes>();
            res.Success = true;
            res.Data    = _service.GetPriceHistory(req);
            res.Message = "Price history fetched successfully";
            return Ok(res);
        }

        [HttpPost("~/api/product-batches/{productEdid}/reconciliation/validate-material")]
        public IActionResult ValidateMaterial(string productEdid, [FromBody] ValidateMaterialReq req)
        {
            ApiResponseModel<ValidateMaterialRes> res = new ApiResponseModel<ValidateMaterialRes>();
            res.Success = true;
            res.Data = _service.ValidateMaterial(productEdid, req);
            res.Message = res.Data.IsValid ? "Material is valid." : "Insufficient stock for this material batch.";
            return Ok(res);
        }
        [HttpPost("~/api/product-batches/reconciliation")]
        public IActionResult ReconcileProductBatch([FromBody] ReconcileProductBatchReq req)
        {
            ApiResponseModel<ReconcileProductBatchRes> res = new ApiResponseModel<ReconcileProductBatchRes>();
            res.Success = true;
            res.Data    = _service.ReconcileProductBatch(req);
            res.Message = "Product reconciliation completed successfully.";
            return Ok(res);
        }

        [HttpPost("customer-products")]
        public IActionResult GetProductsByCustomer([FromBody] GetProductsByCustomerReq req)
        {
            ApiResponseModel<PageResult<CustomerProductListItem>> res = new ApiResponseModel<PageResult<CustomerProductListItem>>();
            res.Success = true;
            res.Data = _service.GetProductsByCustomer(req);
            res.Message = "Products fetched successfully";
            return Ok(res);
        }

        [HttpPost("{productEdid}/usage-history/{productBatchEdid}")]
        public IActionResult GetUsageHistory(string productEdid, string productBatchEdid, [FromBody] GetProductUsageHistoryReq? req = null)
        {
            req ??= new GetProductUsageHistoryReq();
            req.ProductEdid = productEdid;
            req.ProductBatchEdid = productBatchEdid;

            ApiResponseModel<ProductUsageHistoryRes> res = new ApiResponseModel<ProductUsageHistoryRes>();
            res.Success = true;
            res.Data = _service.GetUsageHistory(req);
            res.Message = "Usage history fetched successfully";

            return Ok(res);
        }

        [HttpPost("{productEdid}/batch-list")]
        public IActionResult GetProductBatchList(string productEdid, [FromBody] GetProductBatchListReq req)
        {
            req.ProductEdid = productEdid;

            ApiResponseModel<PageResult<ProductBatchListItem>> res = new ApiResponseModel<PageResult<ProductBatchListItem>>();
            res.Success = true;
            res.Data = _service.GetProductBatchList(req);
            res.Message = "Product batch list fetched successfully";

            return Ok(res);
        }
    }
}