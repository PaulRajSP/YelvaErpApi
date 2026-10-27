using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace HoneyErpAPI.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrderPaymentController : ControllerBase
    {
        private readonly OrderPaymentService _service;
        private readonly ILogger<OrderPaymentController> _logger;

        public OrderPaymentController(OrderPaymentService service, ILogger<OrderPaymentController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        /// <summary>
        /// Records a payment for a specific order.
        /// Automatically recalculates amountPaid, balanceAmount, and updates OrderMaster.paymentStatus.
        /// Optionally synchronizes to general financial ledger (PaymentTransactionMaster).
        /// </summary>
        [HttpPost("{orderEdid}/payments")]
        public IActionResult RecordPayment(string orderEdid, [FromBody] RecordOrderPaymentReq req)
        {
            ApiResponseModel<RecordOrderPaymentRes> res = new ApiResponseModel<RecordOrderPaymentRes>();
            res.Success = true;
            res.Data = _service.RecordPayment(orderEdid, req);
            res.Message = "Payment recorded successfully";
            return Ok(res);
        }

        /// <summary>
        /// Retrieves all payments, total paid, and balance due for a specific order.
        /// </summary>
        [HttpGet("{orderEdid}/payments")]
        public IActionResult GetPayments(string orderEdid)
        {
            ApiResponseModel<GetOrderPaymentsRes> res = new ApiResponseModel<GetOrderPaymentsRes>();
            res.Success = true;
            res.Data = _service.GetPaymentsByOrderId(orderEdid);
            res.Message = "Payments fetched successfully";
            return Ok(res);
        }

        /// <summary>
        /// Deletes/voids an order payment and recalculates the order payment status.
        /// </summary>
        [HttpDelete("payments/{paymentEdid}")]
        public IActionResult DeletePayment(string paymentEdid)
        {
            ApiResponseModel<DeleteOrderPaymentRes> res = new ApiResponseModel<DeleteOrderPaymentRes>();
            res.Success = true;
            res.Data = _service.DeletePayment(paymentEdid);
            res.Message = "Payment deleted successfully";
            return Ok(res);
        }
    }
}
