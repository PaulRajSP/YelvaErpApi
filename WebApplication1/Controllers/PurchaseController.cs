using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Services;
using Microsoft.AspNetCore.Mvc;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Controllers
{
    [ApiController]
    [Route("api/purchases")]
    public class PurchaseController : ControllerBase
    {
        private readonly PurchaseService _service;
        private readonly ILogger<PurchaseController> _logger;

        public PurchaseController(PurchaseService service, ILogger<PurchaseController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        [HttpPost("list")]
        public IActionResult GetAll([FromBody] GetPurchaseListReq req)
        {
            ApiResponseModel<PageResult<PurchaseListItem>> res = new ApiResponseModel<PageResult<PurchaseListItem>>();
            res.Success = true;
            res.Data = _service.GetAll(req);
            res.Message = "Purchases fetched successfully";
            return Ok(res);
        }

        [HttpPost("supplier")]
        public IActionResult GetBySupplier([FromBody] GetPurchasesBySupplierReq req)
        {
            ApiResponseModel<GetPurchaseListRes> res = new ApiResponseModel<GetPurchaseListRes>();
            res.Success = true;
            res.Data    = _service.GetBySupplier(req);
            res.Message = "Purchases fetched successfully";
            return Ok(res);
        }

        [HttpGet("{edid}")]
        public IActionResult GetById(string edid)
        {
            ApiResponseModel<GetPurchaseByIdRes> res = new ApiResponseModel<GetPurchaseByIdRes>();
            res.Success = true;
            res.Data    = _service.GetById(edid);
            res.Message = "Purchase fetched successfully";
            return Ok(res);
        }

        [HttpPost("pay-due")]
        public IActionResult PayDue([FromBody] PayPurchaseDueReq req)
        {
            ApiResponseModel<PayPurchaseDueRes> res = new ApiResponseModel<PayPurchaseDueRes>();
            res.Success = true;
            res.Data = _service.PayDue(req);
            res.Message = "Payment recorded successfully";
            return Ok(res);
        }

        [HttpPost("create")]
        public IActionResult Create([FromBody] CreatePurchaseReq req)
        {
            ApiResponseModel<CreatePurchaseRes> res = new ApiResponseModel<CreatePurchaseRes>();
            res.Success = true;
            res.Data    = _service.Create(req);
            res.Message = "Purchase created successfully";
            return Ok(res);
        }

        [HttpPatch("{edid}/status")]
        public IActionResult UpdateStatus(string edid, [FromBody] string status)
        {
            ApiResponseModel<PurchaseActionRes> res = new ApiResponseModel<PurchaseActionRes>();
            _service.UpdateStatus(edid, status);
            res.Success = true;
            res.Message = "Purchase status updated successfully";
            return Ok(res);
        }

        [HttpPost("items")]
        public IActionResult GetItems([FromBody] GetPurchaseItemsReq req)
        {
            ApiResponseModel<GetPurchaseItemsRes> res = new ApiResponseModel<GetPurchaseItemsRes>();
            res.Success = true;
            res.Data    = _service.GetItems(req);
            res.Message = "Purchase items fetched successfully";
            return Ok(res);
        }

    }
}