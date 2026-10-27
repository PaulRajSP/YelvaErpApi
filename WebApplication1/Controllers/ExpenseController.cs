using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Services;
using Microsoft.AspNetCore.Mvc;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Controllers
{
    [ApiController]
    [Route("api/expenses")]
    public class ExpenseController : ControllerBase
    {
        private readonly ExpenseService _service;
        private readonly ILogger<ExpenseController> _logger;

        public ExpenseController(ExpenseService service, ILogger<ExpenseController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        [HttpPost("list")]
        public IActionResult GetAll([FromBody] GetExpenseListReq req)
        {
            var res = new ApiResponseModel<PageResult<ExpenseItemDto>>
            {
                Success = true,
                Data = _service.GetAll(req),
                Message = "Expenses fetched successfully"
            };
            return Ok(res);
        }

        [HttpGet("summary")]
        public IActionResult GetSummary([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            var res = new ApiResponseModel<ExpenseSummaryRes>
            {
                Success = true,
                Data = _service.GetSummary(fromDate, toDate),
                Message = "Expense summary fetched successfully"
            };
            return Ok(res);
        }

        [HttpGet("{edid}")]
        public IActionResult GetById(string edid)
        {
            var res = new ApiResponseModel<ExpenseItemDto>
            {
                Success = true,
                Data = _service.GetById(edid),
                Message = "Expense fetched successfully"
            };
            return Ok(res);
        }

        [HttpPost]
        public IActionResult Create([FromBody] CreateExpenseReq req)
        {
            string edid = _service.Create(req);
            var res = new ApiResponseModel<CreateExpenseRes>
            {
                Success = true,
                Data = new CreateExpenseRes { Edid = edid },
                Message = "Expense recorded successfully"
            };
            return Ok(res);
        }

        [HttpPut("{edid}")]
        public IActionResult Update(string edid, [FromBody] UpdateExpenseReq req)
        {
            _service.Update(edid, req);
            var res = new ApiResponseModel<UpdateExpenseRes>
            {
                Success = true,
                Data = new UpdateExpenseRes(),
                Message = "Expense updated successfully"
            };
            return Ok(res);
        }

        [HttpDelete("{edid}")]
        public IActionResult Delete(string edid)
        {
            _service.Delete(edid);
            var res = new ApiResponseModel<DeleteExpenseRes>
            {
                Success = true,
                Data = new DeleteExpenseRes(),
                Message = "Expense deleted successfully"
            };
            return Ok(res);
        }
    }
}
