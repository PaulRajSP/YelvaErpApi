using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Services;
using Microsoft.AspNetCore.Mvc;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrderController : ControllerBase
    {
        private readonly OrderService _service;
        private readonly ILogger<OrderController> _logger;

        public OrderController(OrderService service, ILogger<OrderController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        [HttpPost("list")]
        public IActionResult GetAll([FromBody] GetOrderListReq req)
        {
            ApiResponseModel<PageResult<OrderListItem>> res = new ApiResponseModel<PageResult<OrderListItem>>();
            res.Success = true;
            res.Data    = _service.GetAll(req);
            res.Message = "Orders fetched successfully";
            return Ok(res);
        }

        [HttpGet("statuses")]
        public IActionResult GetOrderStatuses()
        {
            var items = Enum.GetNames(typeof(OrderStatus)).ToList();
            return Ok(new { items });
        }

        [HttpGet("{edid}")]
        public IActionResult GetById(string edid)
        {
            ApiResponseModel<GetOrderByIdRes> res = new ApiResponseModel<GetOrderByIdRes>();
            res.Success = true;
            res.Data    = _service.GetById(edid);
            res.Message = "Order fetched successfully";
            return Ok(res);
        }

        [HttpPost("create")]
        public IActionResult Create([FromBody] CreateOrderReq req)
        {
            ApiResponseModel<CreateOrderRes> res = new ApiResponseModel<CreateOrderRes>();
            res.Success = true;
            res.Data    = _service.Create(req);
            res.Message = "Order created successfully";
            return Ok(res);
        }

        [HttpPatch("{edid}/status")]
        public IActionResult UpdateStatus(string edid, [FromBody] System.Text.Json.JsonElement body)
        {
            ApiResponseModel<OrderActionRes> res = new ApiResponseModel<OrderActionRes>();
            string statusStr = "";
            string? remarks = null;
            string? changedBy = null;

            if (body.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                statusStr = body.GetString() ?? "";
            }
            else if (body.ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                int val = body.GetInt32();
                if (Enum.IsDefined(typeof(OrderStatus), val))
                    statusStr = ((OrderStatus)val).ToString();
            }
            else if (body.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                if (body.TryGetProperty("status", out var sp))
                    statusStr = sp.GetString() ?? "";
                if (body.TryGetProperty("remarks", out var rp))
                    remarks = rp.GetString();
                if (body.TryGetProperty("changedBy", out var cp))
                    changedBy = cp.GetString();
            }

            if (!Enum.TryParse<OrderStatus>(statusStr, true, out var orderStatus))
            {
                throw new HoneyErpException(ErrorCode.InvalidData, $"Invalid order status '{statusStr}'.");
            }

            _service.UpdateStatus(edid, orderStatus, remarks, changedBy);
            res.Success = true;
            res.Message = "Order status updated successfully";
            return Ok(res);
        }

        [HttpGet("{orderNumber}/tracking")]
        public IActionResult GetOrderTracking(string orderNumber, [FromQuery] string? orderEdid = null)
        {
            ApiResponseModel<GetOrderStatusTrackingRes> res = new ApiResponseModel<GetOrderStatusTrackingRes>();
            res.Success = true;
            res.Data    = _service.GetOrderStatusTracking(orderNumber, orderEdid);
            res.Message = "Order status tracking fetched successfully";
            return Ok(res);
        }

        [HttpGet("{orderNumber}/status-history")]
        public IActionResult GetOrderStatusHistory(string orderNumber, [FromQuery] string? orderEdid = null)
        {
            return GetOrderTracking(orderNumber, orderEdid);
        }

        [HttpPatch("{edid}/payment-status")]
        public IActionResult UpdatePaymentStatus(string edid, [FromBody] OrderStatus paymentStatus)
        {
            ApiResponseModel<OrderActionRes> res = new ApiResponseModel<OrderActionRes>();
            _service.UpdatePaymentStatus(edid, paymentStatus);
            res.Success = true;
            res.Message = "Payment status updated successfully";
            return Ok(res);
        }

        [HttpGet("{edid}/items")]
        public IActionResult GetItems(string edid)
        {
            ApiResponseModel<GetOrderItemsRes> res = new ApiResponseModel<GetOrderItemsRes>();
            res.Success = true;
            res.Data    = _service.GetItems(edid);
            res.Message = "Order items fetched successfully";
            return Ok(res);
        }

        [HttpGet("{edid}/comments")]
        public IActionResult GetComments(string edid)
        {
            ApiResponseModel<GetOrderCommentsRes> res = new ApiResponseModel<GetOrderCommentsRes>();
            res.Success = true;
            res.Data    = _service.GetComments(edid);
            res.Message = "Comments fetched successfully";
            return Ok(res);
        }

        [HttpPost("{edid}/comments/add")]
        public IActionResult AddComment(string edid, [FromBody] AddCommentReq req)
        {
            ApiResponseModel<AddCommentRes> res = new ApiResponseModel<AddCommentRes>();
            res.Success = true;
            res.Data    = _service.AddComment(edid, req);
            res.Message = "Comment added successfully";
            return Ok(res);
        }

        [HttpGet("{edid}/package")]
        public IActionResult GetPackage(string edid)
        {
            ApiResponseModel<GetPackageRes> res = new ApiResponseModel<GetPackageRes>();
            res.Success = true;
            res.Data    = _service.GetPackage(edid);
            res.Message = "Package fetched successfully";
            return Ok(res);
        }

        [HttpPost("{edid}/package/save")]
        public IActionResult SavePackage(string edid, [FromBody] SavePackageReq req)
        {
            ApiResponseModel<SavePackageRes> res = new ApiResponseModel<SavePackageRes>();
            res.Success = true;
            res.Data    = _service.SavePackage(edid, req);
            res.Message = "Package saved successfully";
            return Ok(res);
        }

        [HttpGet("{orderNumber}/details")]
        public IActionResult GetOrderDetailsByNumber(string orderNumber)
        {
            ApiResponseModel<GetOrderFullDetailsRes> res = new ApiResponseModel<GetOrderFullDetailsRes>();
            res.Success = true;
            res.Data    = _service.GetOrderFullDetails(orderNumber);
            res.Message = "Order details fetched successfully";
            return Ok(res);
        }

        [HttpGet("{orderNumber}/{oderEdid}")]
        public IActionResult GetOrderFullDetails(string orderNumber, string oderEdid)
        {
            ApiResponseModel<GetOrderFullDetailsRes> res = new ApiResponseModel<GetOrderFullDetailsRes>();
            res.Success = true;
            res.Data    = _service.GetOrderFullDetails(orderNumber, oderEdid);
            res.Message = "Order details fetched successfully";
            return Ok(res);
        }

        [HttpPost("{edid}/confirm")]
        public IActionResult ConfirmOrder(string edid)
        {
            _service.ConfirmOrder(edid);
            ApiResponseModel<OrderActionRes> res = new ApiResponseModel<OrderActionRes>();
            res.Success = true;
            res.Message = "Order confirmed successfully and physical bottles created.";
            return Ok(res);
        }

        [HttpGet("{orderNumber}/bottles")]
        public IActionResult GetOrderBottles(string orderNumber)
        {
            ApiResponseModel<GetOrderBottlesRes> res = new ApiResponseModel<GetOrderBottlesRes>();
            res.Success = true;
            res.Data    = _service.GetOrderBottles(orderNumber);
            res.Message = "Physical bottle list fetched successfully";
            return Ok(res);
        }

        [HttpGet("{orderNumber}/packing")]
        public IActionResult GetOrderPackingDetails(string orderNumber)
        {
            ApiResponseModel<GetOrderPackingDetailsRes> res = new ApiResponseModel<GetOrderPackingDetailsRes>();
            res.Success = true;
            res.Data    = _service.GetOrderPackingDetails(orderNumber);
            res.Message = "Order packing details fetched successfully";
            return Ok(res);
        }

        [HttpPost("packing/confirm")]
        public IActionResult ConfirmPacking([FromBody] ConfirmPackingReq req)
        {
            _service.ConfirmPacking(req);
            ApiResponseModel<OrderActionRes> res = new ApiResponseModel<OrderActionRes>();
            res.Success = true;
            res.Message = "Order packed successfully and marked Ready To Ship.";
            return Ok(res);
        }

        [HttpPost("logistics/dispatch/{orderEdid}")]
        public IActionResult DispatchOrder([FromRoute] string orderEdid, [FromBody] DispatchOrderReq req)
        {
            req.OrderEdid = orderEdid;
            _service.DispatchOrder(req);
            ApiResponseModel<OrderActionRes> res = new ApiResponseModel<OrderActionRes>();
            res.Success = true;
            res.Message = "Order dispatched successfully and marked Shipped.";
            return Ok(res);
        }

        [HttpPost("logistics/ready-to-ship")]
        public IActionResult GetReadyToShipOrders([FromBody] GetOrderListReq req)
        {
            ApiResponseModel<PageResult<OrderListItem>> res = new ApiResponseModel<PageResult<OrderListItem>>();
            res.Success = true;
            res.Data    = _service.GetReadyToShipOrders(req);
            res.Message = "Ready to ship orders fetched successfully";
            return Ok(res);
        }
    }
}