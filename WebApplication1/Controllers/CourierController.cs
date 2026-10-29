using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Services;
using Microsoft.AspNetCore.Mvc;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Controllers
{
    [ApiController]
    [Route("api/couriers")]
    public class CourierController : ControllerBase
    {
        private readonly CourierService _service;
        private readonly ILogger<CourierController> _logger;

        public CourierController(CourierService service, ILogger<CourierController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        [HttpPost("list")]
        public IActionResult Get([FromBody] GetCourierListReq req)
        {
            ApiResponseModel<PageResult<CourierListItem>> res = new ApiResponseModel<PageResult<CourierListItem>>();
            res.Success = true;
            res.Data = _service.GetAll(req);
            res.Message = "Couriers fetched successfully";
            return Ok(res);
        }

        [HttpGet("{Edid}/{CourierName}")]
        public IActionResult GetById(string Edid, string CourierName)
        {
            ApiResponseModel<CourierDetailsRes> res = new ApiResponseModel<CourierDetailsRes>();
            res.Success = true;
            res.Data = _service.GetById(Edid, CourierName);

            return Ok(res);
        }

        [HttpPost]
        public IActionResult Add([FromBody] AddCourierReq req)
        {
            ApiResponseModel<AddCourierRes> res = new ApiResponseModel<AddCourierRes>();
            res.Success = true;
            res.Data    = _service.Add(req);
            res.Message = "Courier created successfully";
            return Ok(res);
        }

        [HttpPut("{Edid}/{CourierName}")]
        public IActionResult Update(string Edid, string CourierName, [FromBody] UpdateCourierReq req)
        {
            ApiResponseModel<UpdateCourierRes> resp = new ApiResponseModel<UpdateCourierRes>();
            req.Edid = Edid;
            resp.Data= _service.Update(req, CourierName);

            resp.Success = true;
            return Ok(resp);
        }
    }
}

