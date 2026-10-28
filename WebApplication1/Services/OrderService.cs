using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Repository;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Services
{
    public class OrderService
    {
        private readonly IOrderRepository _repo;
        private readonly ICurrentUserService _currentUserService;

        public OrderService(IOrderRepository repo, ICurrentUserService currentUserService)
        {
            _repo = repo;
            _currentUserService = currentUserService;
        }

        public PageResult<OrderListItem> GetAll(GetOrderListReq req) => _repo.GetAll(req);

        public GetOrderByIdRes GetById(string edid)
        {
            GetOrderByIdRes? order = _repo.GetById(edid);
            if (order == null) throw new HoneyErpException(ErrorCode.NotFound, "Order not found.");
            return order;
        }

        public CreateOrderRes Create(CreateOrderReq req)
        {
            if (string.IsNullOrWhiteSpace(req.Order.customerEdid))
                throw new HoneyErpException(ErrorCode.InvalidData, "Customer is required.");
            if (req.Items == null || req.Items.Count == 0)
                throw new HoneyErpException(ErrorCode.InvalidData, "Order must have at least one item.");

            string createdBy = _currentUserService.EmployeeCode ?? "EMP001";
            return _repo.Create(req, createdBy);
        }

        public void UpdateStatus(string edid, OrderStatus status)
        {
            string changedBy = _currentUserService.EmployeeCode ?? "EMP001";
            UpdateStatus(edid, status, null, changedBy);
        }

        public void UpdateStatus(string edid, OrderStatus status, string? remarks = null, string? changedBy = null)
        {
            if (!_repo.UpdateStatus(edid, status, remarks, changedBy))
                throw new HoneyErpException(ErrorCode.NotFound, "Order not found.");
        }

        public GetOrderStatusTrackingRes GetOrderStatusTracking(string orderEdid)
        {
            return _repo.GetOrderStatusTracking(orderEdid);
        }

        public void UpdatePaymentStatus(string edid, OrderStatus paymentStatus)
        {
            if (!_repo.UpdatePaymentStatus(edid, paymentStatus))
                throw new HoneyErpException(ErrorCode.NotFound, "Order not found.");
        }

        public GetOrderItemsRes GetItems(string orderEdid) => _repo.GetItems(orderEdid);

        public GetOrderCommentsRes GetComments(string orderEdid) => _repo.GetComments(orderEdid);

        public AddCommentRes AddComment(string orderEdid, AddCommentReq req)
        {
            string id = _repo.AddComment(orderEdid, req);
            return new AddCommentRes { Edid = EncryptionHelper.Encrypt(id) };
        }

        public GetPackageRes GetPackage(string orderEdid)
        {
            GetPackageRes? pkg = _repo.GetPackage(orderEdid);
            return pkg ?? new GetPackageRes();
        }

        public SavePackageRes SavePackage(string orderEdid, SavePackageReq req)
        {
            if (string.IsNullOrWhiteSpace(orderEdid))
                throw new HoneyErpException(ErrorCode.InvalidData, "Order EDID is required.");

            if (!req.Weight.HasValue || req.Weight.Value <= 0)
                throw new HoneyErpException(ErrorCode.InvalidData, "Package weight is required and must be greater than zero.");

            if (!req.Length.HasValue || req.Length.Value <= 0)
                throw new HoneyErpException(ErrorCode.InvalidData, "Package length is required and must be greater than zero.");

            if (!req.Width.HasValue || req.Width.Value <= 0)
                throw new HoneyErpException(ErrorCode.InvalidData, "Package width is required and must be greater than zero.");

            if (!req.Height.HasValue || req.Height.Value <= 0)
                throw new HoneyErpException(ErrorCode.InvalidData, "Package height is required and must be greater than zero.");

            return _repo.SavePackage(orderEdid, req);
        }

        public GetOrderFullDetailsRes GetOrderFullDetails(string orderNumber, string? oderEdid = null)
        {
            if (string.IsNullOrWhiteSpace(orderNumber) && string.IsNullOrWhiteSpace(oderEdid))
                throw new HoneyErpException(ErrorCode.InvalidData, "Order number or order ID is required.");

            GetOrderFullDetailsRes? order = _repo.GetOrderFullDetails(orderNumber, oderEdid);
            
            if (order == null)
                throw new HoneyErpException(ErrorCode.NotFound, "Order not found.");

            return order;
        }

        public void ConfirmOrder(string orderEdid)
        {
            if (string.IsNullOrWhiteSpace(orderEdid))
                throw new HoneyErpException(ErrorCode.InvalidData, "Order EDID is required.");

            string confirmedBy = _currentUserService.EmployeeCode ?? "EMP001";

            if (!_repo.ConfirmOrder(orderEdid, confirmedBy))
                throw new HoneyErpException(ErrorCode.NotFound, "Order not found or could not be confirmed.");
        }

        public GetOrderPackingDetailsRes GetOrderPackingDetails(string orderNumber)
        {
            if (string.IsNullOrWhiteSpace(orderNumber))
                throw new HoneyErpException(ErrorCode.InvalidData, "Order number is required.");

            GetOrderPackingDetailsRes? res = _repo.GetOrderPackingDetails(orderNumber);
            if (res == null)
                throw new HoneyErpException(ErrorCode.NotFound, "Order not found.");

            return res;
        }

        public GetOrderBottlesRes GetOrderBottles(string orderNumber)
        {
            if (string.IsNullOrWhiteSpace(orderNumber))
                throw new HoneyErpException(ErrorCode.InvalidData, "Order number is required.");

            return _repo.GetOrderBottles(orderNumber);
        }

        public void ConfirmPacking(ConfirmPackingReq req)
        {
            if (string.IsNullOrWhiteSpace(req.OrderEdid))
                throw new HoneyErpException(ErrorCode.InvalidData, "Order EDID is required.");

            if (!req.Weight.HasValue || req.Weight.Value <= 0)
                throw new HoneyErpException(ErrorCode.InvalidData, "Package weight is required and must be greater than zero.");

            if (!req.Length.HasValue || req.Length.Value <= 0)
                throw new HoneyErpException(ErrorCode.InvalidData, "Package length is required and must be greater than zero.");

            if (!req.Width.HasValue || req.Width.Value <= 0)
                throw new HoneyErpException(ErrorCode.InvalidData, "Package width is required and must be greater than zero.");

            if (!req.Height.HasValue || req.Height.Value <= 0)
                throw new HoneyErpException(ErrorCode.InvalidData, "Package height is required and must be greater than zero.");

            if (req.BottleAssignments == null || req.BottleAssignments.Count == 0)
                throw new HoneyErpException(ErrorCode.InvalidData, "At least one bottle must be assigned a batch.");

            foreach (var b in req.BottleAssignments)
            {
                if (string.IsNullOrWhiteSpace(b.OrderProductItemEdid) || string.IsNullOrWhiteSpace(b.ProductBatchEdid))
                    throw new HoneyErpException(ErrorCode.InvalidData, "All bottles must have a valid batch assigned.");
            }

            if (string.IsNullOrWhiteSpace(req.PackedBy))
            {
                req.PackedBy = _currentUserService.EmployeeCode ?? "EMP001";
            }

            if (!_repo.ConfirmPacking(req))
                throw new HoneyErpException(ErrorCode.InternalError, "Failed to confirm packing.");
        }

        public void DispatchOrder(DispatchOrderReq req)
        {
            if (string.IsNullOrWhiteSpace(req.OrderEdid))
                throw new HoneyErpException(ErrorCode.InvalidData, "Order EDID is required.");

            if (string.IsNullOrWhiteSpace(req.Courier))
                throw new HoneyErpException(ErrorCode.InvalidData, "Courier provider is required.");

            if (string.IsNullOrWhiteSpace(req.Tracking))
                throw new HoneyErpException(ErrorCode.InvalidData, "Tracking / AWB number is required.");

            if (!_repo.DispatchOrder(req))
                throw new HoneyErpException(ErrorCode.InternalError, "Failed to dispatch order.");
        }

        public PageResult<OrderListItem> GetReadyToShipOrders(GetOrderListReq req)
        {
            return _repo.GetReadyToShipOrders(req);
        }

        public List<string> GetOrderNumbersByStatus(string status)
        {
            return _repo.GetOrderNumbersByStatus(status);
        }
    }
}
