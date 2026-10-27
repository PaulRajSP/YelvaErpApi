using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace HoneyErpAPI.Controllers
{
    [ApiController]
    [Route("api/units")]
    public class UnitController : ControllerBase
    {
        private readonly UnitService _service;
        private readonly ILogger<UnitController> _logger;

        public UnitController(UnitService service, ILogger<UnitController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        [HttpPost("list")]
        public IActionResult GetAll()
        {
            ApiResponseModel<GetUnitListRes> res = new ApiResponseModel<GetUnitListRes>();
            res.Success = true;
            res.Data    = _service.GetAll();
            res.Message = "Units fetched successfully";
            return Ok(res);
        }

        [HttpPost]
        public IActionResult Add([FromBody] AddUnitReq req)
        {
            ApiResponseModel<AddUnitRes> res = new ApiResponseModel<AddUnitRes>();
            res.Success = true;
            res.Data = _service.Add(req);
            return Ok(res);
        }

    }
}
