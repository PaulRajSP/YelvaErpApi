using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Services;
using Microsoft.AspNetCore.Mvc;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Controllers
{
    [ApiController]
    [Route("api/suppliers")]
    public class SupplierController : ControllerBase
    {
        private readonly SupplierService _service;
        private readonly ILogger<SupplierController> _logger;

        public SupplierController(SupplierService service, ILogger<SupplierController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        [HttpPost("list")]
        public IActionResult GetAll([FromBody] GetSupplierListReq req)
        {
            ApiResponseModel<PageResult<SupplierListItem>> res = new ApiResponseModel<PageResult<SupplierListItem>>();
            res.Success = true;
            res.Data = _service.GetAll(req);
            res.Message = "Suppliers fetched successfully";
            return Ok(res);
        }

        [HttpGet("{edid}")]
        public IActionResult GetById(string edid)
        {
            ApiResponseModel<GetSupplierByIdRes> res = new ApiResponseModel<GetSupplierByIdRes>();
            res.Success = true;
            res.Data = _service.GetById(edid);
            res.Message = "Supplier fetched successfully";
            return Ok(res);
        }

        [HttpPost]
        public IActionResult AddSupplier([FromBody] AddSupplierReq req)
        {
            ApiResponseModel<AddSupplierRes> res = new ApiResponseModel<AddSupplierRes>();
            res.Success = true;
            res.Data = _service.AddSupplier(req);
            res.Message = "Supplier added successfully";
            return Ok(res);
        }

        [HttpPut("{edid}")]
        public IActionResult Edit(string edid, [FromBody] EditSupplierReq req)
        {
            ApiResponseModel<EditSupplierRes> res = new ApiResponseModel<EditSupplierRes>();
            res.Success = true;
            res.Data = _service.Edit(edid, req);
            res.Message = "Supplier updated successfully";
            return Ok(res);
        }
    }
}