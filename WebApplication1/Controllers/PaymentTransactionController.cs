using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Services;
using Microsoft.AspNetCore.Mvc;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Controllers
{
    [ApiController]
    [Route("api/payment-transactions")]
    public class PaymentTransactionController : ControllerBase
    {
        private readonly PaymentTransactionService _service;

        public PaymentTransactionController(PaymentTransactionService service)
        {
            _service = service;
        }

        [HttpPost("list")]
        public IActionResult GetAll([FromBody] GetPaymentTransactionListReq req)
        {
            ApiResponseModel<PageResult<PaymentTransactionListItem>> res = new ApiResponseModel<PageResult<PaymentTransactionListItem>>();
            res.Success = true;
            res.Data = _service.GetAll(req);
            res.Message = "Transactions fetched successfully";
            return Ok(res);
        }

        [HttpGet("balance")]
        public IActionResult GetBalance()
        {
            ApiResponseModel<GetPaymentTransactionBalanceRes> res = new ApiResponseModel<GetPaymentTransactionBalanceRes>();
            res.Success = true;
            res.Data = _service.GetBalance();
            res.Message = "Balance fetched successfully";
            return Ok(res);
        }

        [HttpPost("create")]
        public IActionResult Create([FromBody] CreatePaymentTransactionReq req)
        {
            ApiResponseModel<CreatePaymentTransactionRes> res = new ApiResponseModel<CreatePaymentTransactionRes>();
            res.Success = true;
            res.Data = _service.Create(req);
            res.Message = "Transaction created successfully";
            return Ok(res);
        }
    }
}
