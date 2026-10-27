using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Services;
using Microsoft.AspNetCore.Mvc;

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
            ApiResponseModel<GetCourierListRes> res = new ApiResponseModel<GetCourierListRes>();
            res.Success = true;
            res.Data = _service.GetAll(req);
            res.Message = "Couriers fetched successfully";
            return Ok(res);
        }

        [HttpGet("{id}")]
        public IActionResult GetById(string id)
        {
            var courier = _service.GetById(id);
            if (courier == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Courier not found"
                });
            }

            return Ok(new ApiResponseModel<CourierListItem>
            {
                Success = true,
                Data = courier,
                Message = "Courier fetched successfully"
            });
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

        [HttpPut("{Edid}")]
        public IActionResult Update([FromBody] UpdateCourierReq req)
        {
            _service.Update(req);


            return Ok(new ApiResponseModel<UpdateCourierRes>
            {
                Success = true,
                Data = new UpdateCourierRes { Success = true },
                Message = "Courier updated successfully"
            });
        }
    }
}

