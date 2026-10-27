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

        public BatchController(BatchService service)
        {
            _service = service;
        }

        [HttpGet("material/{materialEdid}")]
        public IActionResult GetBatches(string materialEdid, [FromQuery] string? batchNumber)
        {
            ApiResponseModel<GetBatchListRes> res = new ApiResponseModel<GetBatchListRes>();
            res.Success = true;
            res.Data    = _service.GetBatches(materialEdid, batchNumber);
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

        [HttpPatch("{edid}/qty")]
        public IActionResult UpdateBatchQty(string edid, [FromBody] decimal qtyRemaining)
        {
            ApiResponseModel<BatchActionRes> res = new ApiResponseModel<BatchActionRes>();
            _service.UpdateBatchQty(edid, qtyRemaining);
            res.Success = true;
            res.Message = "Batch quantity updated successfully";
            return Ok(res);
        }
    }
}
