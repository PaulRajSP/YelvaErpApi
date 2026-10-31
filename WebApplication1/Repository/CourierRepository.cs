using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Repository
{
    public interface ICourierRepository
    {
        PageResult<CourierListItem> GetAll(GetCourierListReq req);
        CourierDetailsRes? GetById(int id, string courierName);
        List<CourierMetadataItem> GetMetadataByCourierId(int courierId);
        string Create(AddCourierReq req);
        UpdateCourierRes Update(UpdateCourierReq req,string courierName);
    }

    public class CourierRepository : ICourierRepository
    {
        private readonly DbClient _client;
        private readonly ICurrentUserService _currentUserService;
        public CourierRepository(DbClient client, ICurrentUserService currentUserService)
        {
            _client = client;
            _currentUserService = currentUserService;
        }

        public PageResult<CourierListItem> GetAll(GetCourierListReq req)
        {
            int pageNumber = req.PaginationRequest?.PageNumber > 0 ? req.PaginationRequest.PageNumber : 1;
            int pageSize = req.PaginationRequest?.PageSize > 0 ? req.PaginationRequest.PageSize : 10;
            int offset = (pageNumber - 1) * pageSize;

            SqlParameter[] p = new SqlParameter[5];
            p[0] = new SqlParameter("@OnlyActive", req.OnlyActive);
            p[1] = new SqlParameter("@SearchText", Utilities.DbValue(req.SearchText?.Trim()));
            p[2] = new SqlParameter("@Offset", offset);
            p[3] = new SqlParameter("@PageSize", pageSize);
            p[4] = new SqlParameter("@IsActive", req.IsActive.HasValue ? req.IsActive.Value : DBNull.Value);

            string query = @"SELECT c.id, c.name, c.contactPerson, c.mobile, c.isActive
            FROM CourierMaster c
            WHERE ((@IsActive IS NOT NULL AND c.isActive = @IsActive)
                OR (@IsActive IS NULL AND (@OnlyActive = 0 OR c.isActive = 1)))
            AND (@SearchText IS NULL OR c.name LIKE '%' + @SearchText + '%' OR c.contactPerson LIKE '%' + @SearchText + '%' OR c.mobile LIKE '%' + @SearchText + '%')
            ORDER BY c.name ASC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(*) AS TotalCount
            FROM CourierMaster c
            WHERE ((@IsActive IS NOT NULL AND c.isActive = @IsActive)
                OR (@IsActive IS NULL AND (@OnlyActive = 0 OR c.isActive = 1)))
            AND (@SearchText IS NULL OR c.name LIKE '%' + @SearchText + '%' OR c.contactPerson LIKE '%' + @SearchText + '%' OR c.mobile LIKE '%' + @SearchText + '%')";

            DataSet ds = _client.ExecuteAdapter(query, p, CommandType.Text);
            DataTable dt = ds.Tables[0];
            DataTable countDt = ds.Tables[1];

            PageResult<CourierListItem> res = new PageResult<CourierListItem>();

            int totalCount = countDt.Rows[0].Field<int>("TotalCount");

            res.TotalCount = totalCount;
            res.PageNumber = pageNumber;
            res.PageSize = pageSize;

            foreach (DataRow row in dt.Rows)
            {
                CourierListItem courierListItem = new CourierListItem();
                courierListItem.Edid = EncryptionHelper.Encrypt(row.Field<int>("id").ToString());
                courierListItem.Name = row.Field<string>("name") ?? string.Empty;
                courierListItem.ContactPerson = row.Field<string?>("contactPerson");
                courierListItem.Mobile = row.Field<string?>("mobile");
                courierListItem.IsActive = row.Field<bool>("isActive");

                res.Items.Add(courierListItem);
            }

            return res;
        }

        public CourierDetailsRes? GetById(int id, string courierName)
        {
            SqlParameter[] p = new SqlParameter[2];
            p[0] = new SqlParameter("@Id", id);
            p[1] = new SqlParameter("@Name", "%" + courierName.Trim() + "%");

            string query = @"SELECT c.id, c.name, c.contactPerson, c.mobile, c.email, c.address, c.isActive,
                            cm.courierMasterId, cm.label, cm.value, cm.showInSlip
                     FROM CourierMaster c
                     LEFT JOIN CourierMetadata cm ON cm.courierMasterId = c.id
                     WHERE c.id = @Id AND c.name LIKE @Name";

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];
            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];

            CourierDetailsRes res = new CourierDetailsRes();
            res.Edid = EncryptionHelper.Encrypt(id.ToString());
            res.Name = row.Field<string>("name") ?? string.Empty;
            res.ContactPerson = row.Field<string?>("contactPerson");
            res.Mobile = row.Field<string?>("mobile");
            res.Email = row.Field<string?>("email");
            res.Address = row.Field<string?>("address");
            res.IsActive = row.Field<bool>("isActive");
            res.Metadata = new List<CourierMetadataItem>();

            foreach (DataRow metadataRow in dt.Rows)
            {
                if (metadataRow.IsNull("courierMasterId"))
                    continue;

                CourierMetadataItem item = new CourierMetadataItem();
                item.CourierMasterId = metadataRow.Field<int>("courierMasterId");
                item.Label = metadataRow.Field<string>("label") ?? string.Empty;
                item.Value = metadataRow.Field<string>("value") ?? string.Empty;
                item.ShowInSlip = metadataRow.Field<bool>("showInSlip");

                res.Metadata.Add(item);
            }

            return res;
        }

        public List<CourierMetadataItem> GetMetadataByCourierId(int courierId)
        {
            SqlParameter[] p = new SqlParameter[1];
            p[0] = new SqlParameter("@CourierId", courierId);

            string query = @"SELECT courierMasterId, label, value, showInSlip
                     FROM CourierMetadata
                     WHERE courierMasterId = @CourierId";

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];
            List<CourierMetadataItem> list = new List<CourierMetadataItem>();

            foreach (DataRow row in dt.Rows)
            {
                CourierMetadataItem item = new CourierMetadataItem();
                item.CourierMasterId = row.Field<int>("courierMasterId");
                item.Label = row.Field<string>("label") ?? string.Empty;
                item.Value = row.Field<string>("value") ?? string.Empty;
                item.ShowInSlip = row.Field<bool>("showInSlip");

                list.Add(item);
            }

            return list;
        }

        public string Create(AddCourierReq req)
        {
            SqlParameter[] p = new SqlParameter[5];
            p[0] = new SqlParameter("@Name", req.Name.Trim());
            p[1] = new SqlParameter("@ContactPerson", Utilities.DbValue(req.ContactPerson));
            p[2] = new SqlParameter("@Mobile", Utilities.DbValue(req.Mobile));
            p[3] = new SqlParameter("@Email", Utilities.DbValue(req.Email));
            p[4] = new SqlParameter("@Address", Utilities.DbValue(req.Address));

            string query = @"INSERT INTO CourierMaster (name, contactPerson, mobile, email, address) OUTPUT INSERTED.id 
                            VALUES (@Name, @ContactPerson, @Mobile, @Email, @Address)";

            int newId = _client.ExecuteScalar<int>(query, p, CommandType.Text);

            if (req.Metadata != null && req.Metadata.Count > 0)
            {
                foreach (CourierMetadataItem meta in req.Metadata)
                {
                    if (string.IsNullOrWhiteSpace(meta.Label) || string.IsNullOrWhiteSpace(meta.Value))
                        continue;

                    SqlParameter[] mp = new SqlParameter[5];
                    mp[0] = new SqlParameter("@CourierMasterId", newId);
                    mp[1] = new SqlParameter("@Label", meta.Label.Trim());
                    mp[2] = new SqlParameter("@Value", meta.Value.Trim());
                    mp[3] = new SqlParameter("@ShowInSlip", meta.ShowInSlip);
                    mp[4] = new SqlParameter("@CreatedBy", _currentUserService.EmployeeCode);

                    string mQuery = @"INSERT INTO CourierMetadata (courierMasterId, label, value, showInSlip, createdBy) 
                              VALUES (@CourierMasterId, @Label, @Value, @ShowInSlip, @CreatedBy)";

                    _client.ExecuteNonQuery(mQuery, mp, CommandType.Text);
                }
            }

            return EncryptionHelper.Encrypt(newId.ToString());
        }

        public UpdateCourierRes Update(UpdateCourierReq req, string courierName)
        {
            int courierId = EncryptionHelper.Decrypt<int>(req.Edid);

            SqlParameter[] p = new SqlParameter[8];
            p[0] = new SqlParameter("@Id", courierId);
            p[1] = new SqlParameter("@Name", req.Name.Trim());
            p[2] = new SqlParameter("@ContactPerson", Utilities.DbValue(req.ContactPerson));
            p[3] = new SqlParameter("@Mobile", Utilities.DbValue(req.Mobile));
            p[4] = new SqlParameter("@Email", Utilities.DbValue(req.Email));
            p[5] = new SqlParameter("@Address", Utilities.DbValue(req.Address));
            p[6] = new SqlParameter("@IsActive", req.IsActive);
            p[7] = new SqlParameter("@CourierName", courierName.Trim());

            string updateQuery = @"UPDATE CourierMaster SET name = @Name, contactPerson = @ContactPerson, mobile = @Mobile, email = @Email, 
                   address = @Address, isActive = @IsActive WHERE id = @Id AND name = @CourierName";

            int rowsAffected = _client.ExecuteNonQuery(updateQuery, p, CommandType.Text);

            if (rowsAffected == 0)
            {
                return new UpdateCourierRes { Success = false };
            }

            if (req.Metadata != null)
            {
                SqlParameter[] dp = new SqlParameter[1];
                dp[0] = new SqlParameter("@CourierMasterId", courierId);

                _client.ExecuteNonQuery("DELETE FROM CourierMetadata WHERE courierMasterId = @CourierMasterId", dp, CommandType.Text);

                foreach (CourierMetadataItem meta in req.Metadata)
                {
                    if (string.IsNullOrWhiteSpace(meta.Label) || string.IsNullOrWhiteSpace(meta.Value))
                        continue;

                    SqlParameter[] mp = new SqlParameter[6];
                    mp[0] = new SqlParameter("@CourierMasterId", courierId);
                    mp[1] = new SqlParameter("@Label", meta.Label.Trim().ToUpper());
                    mp[2] = new SqlParameter("@Value", meta.Value.Trim());
                    mp[3] = new SqlParameter("@ShowInSlip", meta.ShowInSlip);
                    mp[4] = new SqlParameter("@CreatedBy", _currentUserService.EmployeeCode);
                    mp[5] = new SqlParameter("@UpdatedBy", _currentUserService.EmployeeCode);

                    string mQuery = @"INSERT INTO CourierMetadata (courierMasterId, label, value, showInSlip, createdAt, createdBy, updatedAt, updatedBy) 
                      VALUES (@CourierMasterId, @Label, @Value, @ShowInSlip, SYSUTCDATETIME(), @CreatedBy, SYSUTCDATETIME(), @UpdatedBy)";

                    _client.ExecuteNonQuery(mQuery, mp, CommandType.Text);
                }
            }

            UpdateCourierRes res = new UpdateCourierRes();
            res.Success = true;

            return res;
        }
    }
}
