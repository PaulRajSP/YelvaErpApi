using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace HoneyErpAPI.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly DashboardService _service;

        public DashboardController(DashboardService service)
        {
            _service = service;
        }

        [HttpGet("summary")]
        public IActionResult GetSummary([FromQuery] string? period = "today", [FromQuery] string? date = null, [FromQuery] int? timezoneOffset = null)
        {
            var data = _service.GetSummary(period, date, timezoneOffset);
            return Ok(new ApiResponseModel<DashboardTodaySummaryRes>
            {
                Success = true,
                Message = "Dashboard summary fetched successfully",
                Data = data
            });
        }

        [HttpGet("today")]
        public IActionResult GetTodaySummary([FromQuery] string? date = null, [FromQuery] int? timezoneOffset = null)
        {
            return GetSummary("today", date, timezoneOffset);
        }

        [HttpGet("sales-report")]
        public IActionResult GetSalesReport([FromQuery] string? period = "1month", [FromQuery] string? date = null, [FromQuery] int? timezoneOffset = null)
        {
            var data = _service.GetSalesReport(period, date, timezoneOffset);
            return Ok(new ApiResponseModel<SalesReportRes>
            {
                Success = true,
                Message = "Sales report fetched successfully",
                Data = data
            });
        }

        [HttpGet("top-selling")]
        public IActionResult GetTopSellingProducts([FromQuery] string? period = "today", [FromQuery] string? date = null, [FromQuery] int? timezoneOffset = null)
        {
            var data = _service.GetTopSellingProducts(period, date, timezoneOffset);
            return Ok(new ApiResponseModel<TopSellingProductsRes>
            {
                Success = true,
                Message = "Top-selling products fetched successfully",
                Data = data
            });
        }
    }
}
