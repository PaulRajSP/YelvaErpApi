using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Services;
using Microsoft.AspNetCore.Mvc;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Controllers
{
    [ApiController]
    [Route("api/production-requests")]
    [Route("api/production/requests")]
    public class ProductionRequestController : ControllerBase
    {
        private readonly ProductionRequestService _service;
        private readonly ILogger<ProductionRequestController> _logger;

        public ProductionRequestController(ProductionRequestService service, ILogger<ProductionRequestController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        [HttpPost("list")]
        public IActionResult GetAll([FromBody] GetProductionRequestListReq req)
        {
            ApiResponseModel<PageResult<ProductionRequestListItem>> res = new ApiResponseModel<PageResult<ProductionRequestListItem>>();
            res.Success = true;
            res.Data    = _service.GetAll(req);
            res.Message = "Production requests fetched successfully";
            return Ok(res);
        }

        [HttpGet("{edid}")]
        public IActionResult GetById(string edid)
        {
            ApiResponseModel<GetProductionRequestByIdRes> res = new ApiResponseModel<GetProductionRequestByIdRes>();
            res.Success = true;
            res.Data    = _service.GetById(edid);
            res.Message = "Production request fetched successfully";
            return Ok(res);
        }

        [HttpPost("create")]
        public IActionResult CreateProductionRequest([FromBody] CreateProductionRequestReq req)
        {
            ApiResponseModel<CreateProductionRequestRes> res = new ApiResponseModel<CreateProductionRequestRes>();
            res.Success = true;
            res.Data = _service.CreateProductionRequest(req);
            res.Message = "Production request created successfully";
            return Ok(res);
        }

        [HttpPatch("{edid}/status")]
        public IActionResult UpdateProductionRequestStatus(string edid, [FromBody] UpdateProductionRequestStatusReq req)
        {
            ApiResponseModel<ProductionRequestActionRes> res = new ApiResponseModel<ProductionRequestActionRes>();
            req.Edid = edid;
            res.Success = true;
            res.Data = _service.UpdateProductionRequestStatus(req);
            res.Message = "Production request status updated successfully";
            return Ok(res);
        }

        [HttpGet("raw-materials/{productEdid}/recipe")]
        public IActionResult GetRecipeRawMaterials(string productEdid)
        {
            ApiResponseModel<GetRecipeRawMaterialsRes> res = new ApiResponseModel<GetRecipeRawMaterialsRes>();
            res.Success = true;
            res.Data = _service.GetRecipeRawMaterials(productEdid);
            res.Message = "Raw materials recipe fetched successfully";
            return Ok(res);
        }
    }
}
