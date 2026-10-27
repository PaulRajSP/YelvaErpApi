using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Services;
using Microsoft.AspNetCore.Mvc;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Controllers
{
    [ApiController]
    [Route("api/transactions")]
    public class TransactionController : ControllerBase
    {
        private readonly TransactionService _service;
        private readonly ILogger<TransactionController> _logger;

        public TransactionController(TransactionService service, ILogger<TransactionController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        [HttpPost]
        public IActionResult GetAll([FromBody] GetTransactionListReq req)
        {
            ApiResponseModel<PageResult<TransactionListItem>> res = new ApiResponseModel<PageResult<TransactionListItem>>();
            res.Success = true;
            res.Data    = _service.GetAll(req);
            res.Message = "Transactions fetched successfully";
            return Ok(res);
        }

        [HttpPost("category/{category}")]
        public IActionResult GetByCategory(string category, [FromBody] GetTransactionListReq req)
        {
            ApiResponseModel<PageResult<TransactionListItem>> res = new ApiResponseModel<PageResult<TransactionListItem>>();
            res.Success = true;
            res.Data    = _service.GetByCategory(category, req);
            res.Message = "Transactions fetched successfully";
            return Ok(res);
        }

        [HttpGet("{edid}")]
        public IActionResult GetById(string edid)
        {
            ApiResponseModel<GetTransactionByIdRes> res = new ApiResponseModel<GetTransactionByIdRes>();
            res.Success = true;
            res.Data    = _service.GetById(edid);
            res.Message = "Transaction fetched successfully";
            return Ok(res);
        }

        [HttpPost("create")]
        public IActionResult Create([FromBody] CreateTransactionReq req)
        {
            ApiResponseModel<CreateTransactionRes> res = new ApiResponseModel<CreateTransactionRes>();
            res.Success = true;
            res.Data    = _service.Create(req);
            res.Message = "Transaction created successfully";
            return Ok(res);
        }
    }
}