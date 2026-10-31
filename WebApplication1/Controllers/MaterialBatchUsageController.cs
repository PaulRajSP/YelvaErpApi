using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Services;
using Microsoft.AspNetCore.Mvc;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Controllers
{
    [ApiController]
    [Route("api/raw-materials")]
    [Route("api/material-batch-usage")]
    public class MaterialBatchUsageController : ControllerBase
    {
        private readonly MaterialBatchUsageService _service;

        public MaterialBatchUsageController(MaterialBatchUsageService service) => _service = service;

        [HttpPost("{materialEdid}/usage-history/{materialBatchEdid}")]
        public IActionResult GetUsageHistory(string materialEdid, string materialBatchEdid)
        {
            GetMaterialUsageHistoryReq req = new()
            {
                MaterialEdid = materialEdid,
                MaterialBatchEdid = materialBatchEdid
            };

            ApiResponseModel<MaterialUsageHistoryRes> res = new()
            {
                Success = true,
                Data = _service.GetUsageHistory(req),
                Message = "Usage history fetched successfully"
            };

            return Ok(res);
        }

        [HttpPost("{materialEdid}/batch-list")]
        public IActionResult GetMaterialBatchList(string materialEdid, [FromBody] GetMaterialBatchListReq req)
        {
            req.materialEdid = materialEdid;

            ApiResponseModel<PageResult<GetMaterialBatchListItem>> res = new()
            {
                Success = true,
                Data = _service.GetMaterialBatchList(req),
                Message = "Material batch list fetched successfully"
            };

            return Ok(res);
        }
    }
}
