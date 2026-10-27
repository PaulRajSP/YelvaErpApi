using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Repository;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Services
{
    public class customerService
    {
        private readonly IcustomerRepository _repo;
        private readonly ICurrentUserService _currentUserService;

        public customerService(IcustomerRepository repo, ICurrentUserService currentUserService)
        {
            _repo = repo;
            _currentUserService = currentUserService;
        }

        public PageResult<CustomerListItem> GetAllCustomer(GetCustomerListReq req) => _repo.GetAllCustomer(req);

        public GetCustomerByIdRes GetCustomerById(string edid,string mobileNumber)
        {
            GetCustomerByIdRes? customer = _repo.GetCustomerById(edid, mobileNumber);
            if (customer == null) throw new HoneyErpException(ErrorCode.NotFound, "Customer not found.");
            return customer;
        }

        public GetCustomerByMobileNumber GetCustomerByMobile(string mobile)
        {
            GetCustomerByMobileNumber? customer = _repo.GetCustomerByMobile(mobile);
            return customer;
        }

        public CreateCustomerRes CreateCustomer(CreateCustomerReq req)
        {
            if (_repo.IsDuplicatecustomer(req.Mobile))
                throw new HoneyErpException(ErrorCode.InvalidData, "Customer with the same mobile number already exists.");

            Validation.ValidateCreatecustomerRequest(req);

            string createdBy = _currentUserService.EmployeeCode ?? "SYSTEM";
            CreateCustomerRes res = _repo.CreateCustomer(req, createdBy);
            return res;
        }

        public EditCustomerRes EditCustomer(EditCustomerReq req)
        {
            if (_repo.IsDuplicatecustomer(req.Mobile))
                throw new HoneyErpException(ErrorCode.InvalidData, "Customer with the same mobile number already exists.");

            return _repo.EditCustomer(req);
        }

        public GetAddressListRes GetAddresses(string customerEdid) => _repo.GetAddresses(customerEdid);

        public GetAddressByIdRes GetAddressById(string addressEdid)
        {
            GetAddressByIdRes? addr = _repo.GetAddressById(addressEdid);
            if (addr == null) throw new HoneyErpException(ErrorCode.NotFound, "Address not found.");
            return addr;
        }

        public SaveAddressRes SaveAddress(SaveAddressReq req)
        {
            if (string.IsNullOrWhiteSpace(req.RecipientName))
                throw new HoneyErpException(ErrorCode.InvalidData, "Recipient name is required.");
            if (string.IsNullOrWhiteSpace(req.PrimaryMobile))
                throw new HoneyErpException(ErrorCode.InvalidData, "Primary mobile is required.");

            SaveAddressRes resp = _repo.SaveAddress(req);

            return resp;
        }


        public PageResult<CustOrderListItem> GetCustomerOrder(string edid, string mobileNumber, GetCustOrderListReq req) => _repo.GetCustomerOrder(edid, mobileNumber, req);

        public class Validation
        {
            public static void ValidateCreatecustomerRequest(CreateCustomerReq req)
            {
                if (string.IsNullOrWhiteSpace(req.Name))
                    throw new HoneyErpException(ErrorCode.InvalidData, "Customer name is required.");
                if (string.IsNullOrWhiteSpace(req.Mobile))
                    throw new HoneyErpException(ErrorCode.InvalidData, "Customer mobile number is required.");
            }

            public static void ValidateEditcustomerRequest(EditCustomerReq req)
            {
                if (string.IsNullOrWhiteSpace(req.Edid))
                    throw new HoneyErpException(ErrorCode.InvalidData, "Customer ID is required.");
                if (string.IsNullOrWhiteSpace(req.Name))
                    throw new HoneyErpException(ErrorCode.InvalidData, "Customer name is required.");
                if (string.IsNullOrWhiteSpace(req.Mobile))
                    throw new HoneyErpException(ErrorCode.InvalidData, "Customer mobile number is required.");
            }
        }
    }
}