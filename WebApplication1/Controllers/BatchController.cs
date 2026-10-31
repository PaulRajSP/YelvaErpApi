using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace HoneyErpAPI.Controllers
{
    [ApiController]
    [Route("api/batches")]
    public class BatchController : ControllerBase
    {
        private readonly BatchService _service;
        private readonly MaterialBatchUsageService _materialBatchUsageService;

        public BatchController(BatchService service, MaterialBatchUsageService materialBatchUsageService)
        {
            _service = service;
            _materialBatchUsageService = materialBatchUsageService;
        }

        [HttpGet("material/{materialEdid}")]
        public IActionResult GetBatches(string materialEdid, [FromQuery] string? batchNumber, [FromQuery] decimal? unitCost, [FromQuery] string? supplierEdid)
        {
            ApiResponseModel<GetBatchListRes> res = new ApiResponseModel<GetBatchListRes>();
            res.Success = true;
            res.Data    = _service.GetBatches(materialEdid, batchNumber, unitCost, supplierEdid);
            res.Message = "Batches fetched successfully";
            return Ok(res);
        }

        [HttpGet("{edid}")]
        public IActionResult GetBatchById(string edid)
        {
            ApiResponseModel<GetBatchByIdRes> res = new ApiResponseModel<GetBatchByIdRes>();
            res.Success = true;
            res.Data    = _service.GetBatchById(edid);
            res.Message = "Batch fetched successfully";
            return Ok(res);
        }

        [HttpPost("create")]
        public IActionResult CreateBatch([FromBody] CreateBatchReq req)
        {
            ApiResponseModel<CreateBatchRes> res = new ApiResponseModel<CreateBatchRes>();
            res.Success = true;
            res.Data    = _service.CreateBatch(req);
            res.Message = "Batch created successfully";
            return Ok(res);
        }

        [HttpPatch("{edid}/adjustment")]
        public IActionResult AdjustBatch(string edid, [FromBody] MaterialBatchAdjustmentReq req)
        {
            ApiResponseModel<BatchActionRes> res = new ApiResponseModel<BatchActionRes>();
            _materialBatchUsageService.AdjustBatch(edid, req);
            res.Success = true;
            res.Message = "Batch quantity adjusted successfully";
            return Ok(res);
        }

        [HttpPatch("{edid}/close")]
        public IActionResult CloseBatch(string edid)
        {
            ApiResponseModel<BatchActionRes> res = new ApiResponseModel<BatchActionRes>();
            _materialBatchUsageService.CloseBatch(edid);
            res.Success = true;
            res.Message = "Batch closed successfully";
            return Ok(res);
        }
    }
}
