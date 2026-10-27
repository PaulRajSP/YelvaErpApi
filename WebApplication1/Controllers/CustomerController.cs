using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Controllers
{
    [ApiController]
    [Route("api/customers")]
    public class customerController : ControllerBase
    {
        private readonly customerService _service;
        private readonly ILogger<customerController> _logger;

        public customerController(customerService service, ILogger<customerController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        [HttpPost("list")]
        public IActionResult GetAllCustomer([FromBody] GetCustomerListReq req)
        {
            ApiResponseModel<PageResult<CustomerListItem>> res = new ApiResponseModel<PageResult<CustomerListItem>>();
            res.Success = true;
            res.Data = _service.GetAllCustomer(req);
            return Ok(res);
        }

        [HttpGet("{edid}/{mobileNumber}")]
        public IActionResult GetCustomerById(string edid,string mobileNumber)
        {
            ApiResponseModel<GetCustomerByIdRes> res = new ApiResponseModel<GetCustomerByIdRes>();
            res.Success = true;
            res.Data = _service.GetCustomerById(edid, mobileNumber);
            return Ok(res);
        }

        [HttpGet("mobile/{mobile}")]
        public IActionResult GetCustomerByMobile(string mobile)
        {
            ApiResponseModel<GetCustomerByMobileNumber> res = new ApiResponseModel<GetCustomerByMobileNumber>();
            res.Success = true;
            res.Data = _service.GetCustomerByMobile(mobile);
            return Ok(res);
        }

        [HttpPost]
        public IActionResult CreateCustomer([FromBody] CreateCustomerReq req)
        {
            ApiResponseModel<CreateCustomerRes> res = new ApiResponseModel<CreateCustomerRes>();
            res.Success = true;
            res.Data = _service.CreateCustomer(req);
            res.Message = "Customer added successfully";
            return Ok(res);
        }

        [HttpPut("{edid}")]
        public IActionResult EditCustomer(string edid, [FromBody] EditCustomerReq req)
        {
            ApiResponseModel<EditCustomerRes> res = new ApiResponseModel<EditCustomerRes>();
            req.Edid = edid;
            res.Data = _service.EditCustomer(req);
            res.Success = true;
            return Ok(res);
        }

        [HttpGet("{customerEdid}/addresses")]
        public IActionResult GetAddresses(string customerEdid)
        {
            ApiResponseModel<GetAddressListRes> res = new ApiResponseModel<GetAddressListRes>();
            res.Success = true;
            res.Data = _service.GetAddresses(customerEdid);
            return Ok(res);
        }

        [HttpGet("{customerEdid}/addresses/{addressEdid}")]
        public IActionResult GetAddressById(string customerEdid, string addressEdid)
        {
            ApiResponseModel<GetAddressByIdRes> res = new ApiResponseModel<GetAddressByIdRes>();
            res.Success = true;
            res.Data = _service.GetAddressById(addressEdid);
            return Ok(res);
        }

        [HttpPost("{customerEdid}/{edid}/addresses/save")]
        public IActionResult SaveAddress(string customerEdid, string edid, [FromBody] SaveAddressReq req)
        {
            ApiResponseModel<SaveAddressRes> res = new ApiResponseModel<SaveAddressRes>();
            res.Success = true;
            req.CustomerEdid = customerEdid;
            req.Edid = edid;
            res.Data = _service.SaveAddress(req);
            res.Message = "Address saved successfully";
            return Ok(res);
        }

        [HttpPost("order/{edid}/{mobileNumber}")]
        public IActionResult GetCustomerOrder(string edid, string mobileNumber, [FromBody] GetCustOrderListReq req)
        {
            ApiResponseModel<PageResult<CustOrderListItem>> res = new ApiResponseModel<PageResult<CustOrderListItem>>();
            res.Success = true;
            res.Data = _service.GetCustomerOrder(edid, mobileNumber, req);
            res.Message = "Customer orders fetched successfully";
            return Ok(res);
        }
    }
}