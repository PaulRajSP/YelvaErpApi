using HoneyErpAPI.Common;
using System.Text.Json.Serialization;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CustomerType{Direct,Wholesale,Online}



    // ── List ─────────────────────────────────────────────────────────────────
    public class GetCustomerListReq : BaseApiRequest
    {
        public PaginationRequest Pagination { get; set; } = new PaginationRequest();
        public string? Mobile     { get; set; }
    }

    public class CustomerListItem
    {
        public string Edid { get; set; }
        public string Name { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public CustomerType? Type { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalSpent  { get; set; }
    }

    // ── Get by Id ────────────────────────────────────────────────────────────
    public class GetCustomerByIdRes : BaseApiResponse
    {
        public string Edid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Mobile { get; set; } 
        public string? Email { get; set; }
        public CustomerType? Type { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalSpent  { get; set; }
    }

    // ── Get by MobileNumber ────────────────────────────────────────────────────────────
    public class GetCustomerByMobileNumber : BaseApiResponse
    {
        public string Edid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public CustomerType? Type { get; set; }
    }


    // ── Create ─────────────────────────────────────────────────────────────────
    public class CreateCustomerReq : BaseApiRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public CustomerType? Type { get; set; }
    }

    public class CreateCustomerRes : BaseApiResponse
    {
        public string Edid { get; set; } = string.Empty;
    }

    public class EditCustomerReq : BaseApiRequest
    {
        [JsonIgnore]
        public string Edid { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public CustomerType? Type { get; set; }
    }

    public class EditCustomerRes : BaseApiResponse { }

    // ── Address List ─────────────────────────────────────────────────────────
    public class GetAddressListRes : BaseApiResponse
    {
        public List<AddressListItem> Items { get; set; } = new();
    }

    public class AddressListItem
    {
        public string  Edid { get; set; } = string.Empty;
        public string  AddressType { get; set; } = string.Empty;
        public string  RecipientName { get; set; } = string.Empty;
        public string  PrimaryMobile { get; set; } = string.Empty;
        public string? SecondaryMobile { get; set; }
        public string  HouseStreet { get; set; } = string.Empty;
        public string? AreaLocality { get; set; }
        public string? Landmark { get; set; }
        public string  City { get; set; } = string.Empty;
        public string  District { get; set; } = string.Empty;
        public string  State { get; set; } = string.Empty;
        public string  Pincode { get; set; } = string.Empty;
        public string? DeliveryInstructions { get; set; }
    }

    // ── Get Address by Id ────────────────────────────────────────────────────
    public class GetAddressByIdRes : BaseApiResponse
    {
        public string  Edid { get; set; } = string.Empty;
        public string  AddressType { get; set; } = string.Empty;
        public string  RecipientName { get; set; } = string.Empty;
        public string  PrimaryMobile { get; set; } = string.Empty;
        public string? SecondaryMobile { get; set; }
        public string  HouseStreet { get; set; } = string.Empty;
        public string? AreaLocality { get; set; }
        public string? Landmark { get; set; }
        public string  City { get; set; } = string.Empty;
        public string  District { get; set; } = string.Empty;
        public string  State { get; set; } = string.Empty;
        public string  Pincode { get; set; } = string.Empty;
        public string? DeliveryInstructions { get; set; }
    }

    // ── Save Address ─────────────────────────────────────────────────────────
    public class SaveAddressReq : BaseApiRequest
    {
        [JsonIgnore]
        public string  Edid { get; set; } = string.Empty;

        [JsonIgnore]
        public string  CustomerEdid { get; set; } = string.Empty;
        public string  AddressType { get; set; } = string.Empty;
        public string  RecipientName { get; set; } = string.Empty;
        public string  PrimaryMobile { get; set; } = string.Empty;
        public string? SecondaryMobile { get; set; }
        public string  HouseStreet { get; set; } = string.Empty;
        public string? AreaLocality { get; set; }
        public string? Landmark { get; set; }
        public string  City { get; set; } = string.Empty;
        public string  District { get; set; } = string.Empty;
        public string  State { get; set; } = string.Empty;
        public string  Pincode { get; set; } = string.Empty;
        public string? DeliveryInstructions { get; set; }
    }

    public class SaveAddressRes : BaseApiResponse
    {
        public string Edid { get; set; } = string.Empty;
    }


    public class GetCustOrderListReq
    {
        public PaginationRequest Pagination { get; set; } = new PaginationRequest();
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? OrderStatus { get; set; }
        public string? PaymentStatus { get; set; }
        public string? OrderNumber { get; set; }
    }

    public class CustOrderListItem
    {
        public string Edid { get; set; }
        public string OrderNumber { get; set; }
        public string CustomerEdid { get; set; }
        public string CustomerName { get; set; }
        public string CustomerMobile { get; set; }
        public string RecipientName { get; set; }
        public string DeliveryType { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; }
        public string OrderStatus { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
