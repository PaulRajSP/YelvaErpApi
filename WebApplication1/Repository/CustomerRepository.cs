using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Repository
{
    public interface IcustomerRepository
    {
        PageResult<CustomerListItem> GetAllCustomer(GetCustomerListReq req);
        GetCustomerByIdRes? GetCustomerById(string edid,string mobileNumber);
        GetCustomerByMobileNumber? GetCustomerByMobile(string mobile);
        CreateCustomerRes CreateCustomer(CreateCustomerReq req, string? createdBy = null);
        EditCustomerRes EditCustomer(EditCustomerReq req);
        GetAddressListRes GetAddresses(string customerEdid);
        GetAddressByIdRes? GetAddressById(string addressEdid);
        SaveAddressRes SaveAddress(SaveAddressReq req);
        PageResult<CustOrderListItem>  GetCustomerOrder(string edid, string mobileNumber, GetCustOrderListReq req);
        bool IsDuplicatecustomer(string mobileNumber);
    }

    public class customerRepository : IcustomerRepository
    {
        private readonly DbClient _client;
        public customerRepository(DbClient client) => _client = client;

        // ── customers ────────────────────────────────────────────────────────

        public PageResult<CustomerListItem> GetAllCustomer(GetCustomerListReq req)
        {
            int pageNumber = req.Pagination.PageNumber > 0 ? req.Pagination.PageNumber : 1;
            int pageSize = req.Pagination.PageSize > 0 ? req.Pagination.PageSize : 10;
            int offset = (pageNumber - 1) * pageSize;

            string query = @"SELECT COUNT(*) OVER() AS TotalRecords,id,name,mobile,email,type,totalOrders,totalSpent
                            FROM CustomerMaster WHERE (@Mobile IS NULL OR mobile = @Mobile)
                            ORDER BY id DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            SqlParameter[] p = new SqlParameter[3];
            p[0] = new SqlParameter("@Mobile", Utilities.DbValue(req.Mobile));
            p[1] = new SqlParameter("@Offset", offset);
            p[2] = new SqlParameter("@PageSize", pageSize);

            DataSet ds = _client.ExecuteAdapter(query, p, CommandType.Text);

            PageResult<CustomerListItem> res = new PageResult<CustomerListItem>
            {
                TotalCount = 0,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                res.TotalCount = Convert.ToInt32(ds.Tables[0].Rows[0]["TotalRecords"]);

                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    CustomerListItem customerListItem = new CustomerListItem();
                    customerListItem.Edid = EncryptionHelper.Encrypt(row.Field<int>("id").ToString());
                    customerListItem.Name = row.Field<string>("name");
                    customerListItem.Mobile = Utilities.CleanMobileNumber(row.Field<string?>("mobile"));
                    customerListItem.Email = row.Field<string?>("email");
                    customerListItem.Type = Utilities.ParseEnum<CustomerType>(row.Field<string?>("type"));
                    customerListItem.TotalOrders = row.Field<int?>("totalOrders") ?? 0;
                    customerListItem.TotalSpent = row.Field<decimal?>("totalSpent") ?? 0m;

                    res.Items.Add(customerListItem);
                }
            }

            return res;
        }

        public GetCustomerByIdRes GetCustomerById(string edid, string mobileNumber)
        {
            SqlParameter[] p = new SqlParameter[2];
            p[0] = new SqlParameter("@Id", EncryptionHelper.Decrypt(edid));
            p[1] = new SqlParameter("@Mobile", mobileNumber);

            string query = "SELECT id,name,mobile,email,type,ISNULL(totalOrders, 0) AS totalOrders,ISNULL(totalSpent, 0) AS totalSpent FROM CustomerMaster WHERE id=@Id AND mobile=@Mobile";
            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            GetCustomerByIdRes resp = new GetCustomerByIdRes();
            resp.Edid = EncryptionHelper.Encrypt(row.Field<int>("id").ToString());
            resp.Name = row.Field<string>("name");
            resp.Mobile = row.Field<string?>("mobile");
            resp.Email = row.Field<string?>("email");
            resp.Type = Utilities.ParseEnum<CustomerType>(row.Field<string?>("type"));
            resp.TotalOrders = row.Field<int>("totalOrders");
            resp.TotalSpent = row.Field<decimal>("totalSpent");

            return resp;
        }
        
        public GetCustomerByMobileNumber? GetCustomerByMobile(string mobile)
        {
            SqlParameter[] p = new SqlParameter[1];
            p[0] =new SqlParameter("@Mobile", mobile);

            string query = "SELECT id,name,mobile,email,type FROM CustomerMaster WHERE mobile=@Mobile";
            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];

            GetCustomerByMobileNumber resp = new GetCustomerByMobileNumber();
            resp.Edid = EncryptionHelper.Encrypt(row.Field<int>("id").ToString());
            resp.Name = row.Field<string>("name");
            resp.Mobile = row.Field<string?>("mobile");
            resp.Email = row.Field<string?>("email");
            resp.Type = Utilities.ParseEnum<CustomerType>(row.Field<string?>("type"));

            return resp;
        }

        public CreateCustomerRes CreateCustomer(CreateCustomerReq req, string? createdBy = null)
        {
            SqlParameter[] p = new SqlParameter[5];
            p[0] = new SqlParameter("@Name",      req.Name);
            p[1] = new SqlParameter("@Mobile",    Utilities.DbValue(req.Mobile));
            p[2] = new SqlParameter("@Email",     Utilities.DbValue(req.Email));
            p[3] = new SqlParameter("@Type",      Utilities.DbValue(req.Type.ToString()));
            p[4] = new SqlParameter("@CreatedBy", Utilities.DbValue(createdBy));

            string query = "INSERT INTO CustomerMaster (name,mobile,email,type,createdBy,createdAt) OUTPUT INSERTED.Id VALUES (@Name,@Mobile,@Email,@Type,@CreatedBy,GETUTCDATE())";

            CreateCustomerRes resp = new CreateCustomerRes();
            resp.Edid = EncryptionHelper.Encrypt(_client.ExecuteScalar<int>(query, p, CommandType.Text).ToString());

            return resp;
        }

        public EditCustomerRes EditCustomer(EditCustomerReq req)
        {
            SqlParameter[] p = new SqlParameter[5];
            p[0] = new SqlParameter("@Id", EncryptionHelper.Decrypt(req.Edid));
            p[1] = new SqlParameter("@Name", req.Name);
            p[2] = new SqlParameter("@Mobile", Utilities.DbValue(req.Mobile));
            p[3] = new SqlParameter("@Email", Utilities.DbValue(req.Email));
            p[4] = new SqlParameter("@Type", Utilities.DbValue(req.Type?.ToString()));

            string query = "UPDATE CustomerMaster SET name=@Name,mobile=@Mobile,email=@Email,type=@Type WHERE id=@Id";
            EditCustomerRes resp = new EditCustomerRes();
            bool rowsAffected = _client.ExecuteNonQuery(query, p, CommandType.Text) > 0;

            return resp;
        }

        // ── Addresses ────────────────────────────────────────────────────────

        public GetAddressListRes GetAddresses(string customerEdid)
        {
            SqlParameter[] p = new SqlParameter[1];
            p[0] = new SqlParameter("@customerId", EncryptionHelper.Decrypt(customerEdid));

            string query = @"SELECT id,addressType,recipientName,primaryMobile,secondaryMobile,houseStreet,areaLocality,
                            landmark,city,district,state,pincode,deliveryInstructions FROM AddressMaster
                            WHERE customerId=@customerId";
            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            GetAddressListRes res = new GetAddressListRes();
            foreach (DataRow row in dt.Rows)
            {
                AddressListItem item = new AddressListItem();
                item.Edid = EncryptionHelper.Encrypt(row["id"].ToString()!);
                item.AddressType = row.Field<string>("addressType")!;
                item.RecipientName = row.Field<string>("recipientName")!;
                item.PrimaryMobile = row.Field<string>("primaryMobile")!;
                item.SecondaryMobile = row.Field<string?>("secondaryMobile");
                item.HouseStreet = row.Field<string>("houseStreet")!;
                item.AreaLocality = row.Field<string?>("areaLocality");
                item.Landmark = row.Field<string?>("landmark");
                item.City = row.Field<string>("city")!;
                item.District = row.Field<string>("district")!;
                item.State = row.Field<string>("state")!;
                item.Pincode = row.Field<string>("pincode")!;
                item.DeliveryInstructions = row.Field<string?>("deliveryInstructions");

                res.Items.Add(item);
            }

            return res;
        }

        public GetAddressByIdRes? GetAddressById(string addressEdid)
        {
            SqlParameter[] p = new SqlParameter[1];
            p[0] = new SqlParameter("@Id", EncryptionHelper.Decrypt(addressEdid));

            string query = @"SELECT id,addressType,recipientName,primaryMobile,secondaryMobile,
                            houseStreet,areaLocality,landmark,city,district,state,pincode,
                            deliveryInstructions FROM AddressMaster WHERE id=@Id";

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            GetAddressByIdRes resp = new GetAddressByIdRes();
            resp.Edid = EncryptionHelper.Encrypt(row["id"].ToString()!);
            resp.AddressType = row.Field<string>("addressType")!;
            resp.RecipientName = row.Field<string>("recipientName")!;
            resp.PrimaryMobile = row.Field<string>("primaryMobile")!;
            resp.SecondaryMobile = row.Field<string?>("secondaryMobile");
            resp.HouseStreet = row.Field<string>("houseStreet")!;
            resp.AreaLocality = row.Field<string?>("areaLocality");
            resp.Landmark = row.Field<string?>("landmark");
            resp.City = row.Field<string>("city")!;
            resp.District = row.Field<string>("district")!;
            resp.State = row.Field<string>("state")!;
            resp.Pincode = row.Field<string>("pincode")!;
            resp.DeliveryInstructions = row.Field<string?>("deliveryInstructions");

            return resp;
        }

        public SaveAddressRes SaveAddress(SaveAddressReq req)
        {
            SqlParameter[] p = new SqlParameter[14];
            p[0] = new SqlParameter("@Id", Utilities.DbValue(string.IsNullOrWhiteSpace(req.Edid) ? null : EncryptionHelper.Decrypt(req.Edid)));
            p[1] = new SqlParameter("@customerId", EncryptionHelper.Decrypt(req.CustomerEdid));
            p[2] = new SqlParameter("@AddressType", req.AddressType);
            p[3] = new SqlParameter("@RecipientName", req.RecipientName);
            p[4] = new SqlParameter("@PrimaryMobile", req.PrimaryMobile);
            p[5] = new SqlParameter("@SecondaryMobile", Utilities.DbValue(req.SecondaryMobile));
            p[6] = new SqlParameter("@HouseStreet", req.HouseStreet);
            p[7] = new SqlParameter("@AreaLocality", Utilities.DbValue(req.AreaLocality));
            p[8] = new SqlParameter("@Landmark", Utilities.DbValue(req.Landmark));
            p[9] = new SqlParameter("@City", req.City);
            p[10] = new SqlParameter("@District", req.District);
            p[11] = new SqlParameter("@State", req.State);
            p[12] = new SqlParameter("@Pincode", req.Pincode);
            p[13] = new SqlParameter("@DeliveryInstructions", Utilities.DbValue(req.DeliveryInstructions));

            string query = @"IF @Id IS NULL BEGIN INSERT INTO AddressMaster 
                             (customerId,addressType,recipientName,primaryMobile,secondaryMobile,houseStreet,areaLocality,
                             landmark,city,district,state,pincode,deliveryInstructions)
                             OUTPUT INSERTED.Id VALUES (@customerId,@AddressType,@RecipientName,@PrimaryMobile,@SecondaryMobile,
                             @HouseStreet,@AreaLocality,@Landmark,@City,@District,@State,@Pincode,@DeliveryInstructions);
                             END ELSE BEGIN UPDATE AddressMaster SET addressType=@AddressType,recipientName=@RecipientName,
                             primaryMobile=@PrimaryMobile,secondaryMobile=@SecondaryMobile,houseStreet=@HouseStreet,
                             areaLocality=@AreaLocality,landmark=@Landmark,city=@City,district=@District,state=@State,
                             pincode=@Pincode,deliveryInstructions=@DeliveryInstructions WHERE id=@Id AND customerId=@customerId; 
                             IF @@ROWCOUNT > 0 SELECT @Id; ELSE SELECT 0; END";

            int id = _client.ExecuteScalar<int>(query, p, CommandType.Text);

            if (id == 0)
                throw new HoneyErpException(ErrorCode.NotFound, "Address not found.");
            
            SaveAddressRes resp = new SaveAddressRes();
            resp.Edid = EncryptionHelper.Encrypt(id.ToString());

            return resp;
        }


        public PageResult<CustOrderListItem> GetCustomerOrder(string edid, string mobileNumber, GetCustOrderListReq req)
        {
            string customerId = EncryptionHelper.Decrypt(edid);

            if (string.IsNullOrWhiteSpace(customerId) || string.IsNullOrWhiteSpace(mobileNumber))
                return new PageResult<CustOrderListItem>();

            int pageNumber = req.Pagination.PageNumber > 0 ? req.Pagination.PageNumber : 1;
            int pageSize = req.Pagination.PageSize > 0 ? req.Pagination.PageSize : 10;
            int offset = (pageNumber - 1) * pageSize;

            SqlParameter[] p = new SqlParameter[9];
            p[0] = new SqlParameter("@CustomerId", customerId);
            p[1] = new SqlParameter("@MobileNumber", mobileNumber);
            p[2] = new SqlParameter("@FromDate", Utilities.DbValue(req.FromDate));
            p[3] = new SqlParameter("@ToDate", Utilities.DbValue(req.ToDate));
            p[4] = new SqlParameter("@OrderStatus", Utilities.DbValue(req.OrderStatus));
            p[5] = new SqlParameter("@PaymentStatus", Utilities.DbValue(req.PaymentStatus));
            p[6] = new SqlParameter("@OrderNumber", Utilities.DbValue(req.OrderNumber));
            p[7] = new SqlParameter("@Offset", offset);
            p[8] = new SqlParameter("@PageSize", pageSize);

            string query = @"SELECT COUNT(*) OVER() AS totalCount, o.id, o.orderNumber, o.customerId, c.name AS customerName, c.mobile AS customerMobile,
                        o.deliveryType, a.recipientName, o.totalAmount, o.paymentStatus, o.status AS orderStatus, o.createdAt
                 FROM OrderMaster o
                 INNER JOIN CustomerMaster c ON o.customerId = c.id
                 LEFT JOIN AddressMaster a ON o.deliveryAddressId = a.id
                 WHERE o.customerId = @CustomerId
                 AND c.mobile = @MobileNumber
                 AND (@FromDate IS NULL OR CAST(o.createdAt AS DATE) >= CAST(@FromDate AS DATE))
                 AND (@ToDate IS NULL OR CAST(o.createdAt AS DATE) <= CAST(@ToDate AS DATE))
                 AND (@OrderStatus IS NULL OR o.status = @OrderStatus)
                 AND (@PaymentStatus IS NULL OR o.paymentStatus = @PaymentStatus)
                 AND (@OrderNumber IS NULL OR o.orderNumber LIKE '%' + @OrderNumber + '%')
                 ORDER BY o.createdAt DESC
                 OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            PageResult<CustOrderListItem> res = new PageResult<CustOrderListItem>
            {
                TotalCount = dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["totalCount"]) : 0,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            foreach (DataRow row in dt.Rows)
            {
                CustOrderListItem item = new CustOrderListItem();
                item.Edid = EncryptionHelper.Encrypt(Convert.ToString(row["id"]));
                item.OrderNumber = row.Field<string>("orderNumber") ?? string.Empty;
                item.CustomerEdid = EncryptionHelper.Encrypt(Convert.ToString(row["customerId"]));
                item.CustomerName = row.Field<string>("customerName") ?? string.Empty;
                item.CustomerMobile = row.Field<string>("customerMobile") ?? string.Empty;
                item.RecipientName = row.Field<string>("recipientName") ?? string.Empty;
                item.DeliveryType = row.Field<string>("deliveryType") ?? string.Empty;
                item.TotalAmount = row.Field<decimal>("totalAmount");
                item.PaymentStatus = row.Field<string>("paymentStatus") ?? string.Empty;
                item.OrderStatus = row.Field<string>("orderStatus") ?? string.Empty;
                item.CreatedAt = row.Field<DateTime>("createdAt");

                res.Items.Add(item);
            }

            return res;
        }


        //Helper method to check if a customer with the same mobile number already exists
        public bool IsDuplicatecustomer(string mobileNumber)
        {
            SqlParameter[] p = new SqlParameter[1];
            p[0] = new SqlParameter("@Mobile", Utilities.DbValue(mobileNumber));

            string query = "SELECT CASE WHEN EXISTS (SELECT 1 FROM CustomerMaster WHERE mobile = @Mobile) THEN 1 ELSE 0 END";

            return _client.ExecuteScalar<int>(query, p, CommandType.Text) == 1;
        }

    }
}