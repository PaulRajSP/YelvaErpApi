using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace HoneyErpAPI.Repository
{
    public interface ICourierRepository
    {
        GetCourierListRes GetAll(GetCourierListReq req);
        CourierListItem? GetById(int id);
        List<CourierMetadataItem> GetMetadataByCourierId(int courierId);
        string Create(AddCourierReq req, int currentUserId = 1);
        bool Update(UpdateCourierReq req);
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

        public GetCourierListRes GetAll(GetCourierListReq req)
        {
            int pageNumber = req.PaginationRequest?.PageNumber > 0 ? req.PaginationRequest.PageNumber : 1;
            int pageSize = req.PaginationRequest?.PageSize > 0 ? req.PaginationRequest.PageSize : 10;
            int offset = (pageNumber - 1) * pageSize;

            SqlParameter[] p = new SqlParameter[4];
            p[0] = new SqlParameter("@OnlyActive", req.OnlyActive);
            p[1] = new SqlParameter("@SearchText", Utilities.DbValue(req.SearchText));
            p[2] = new SqlParameter("@Offset", offset);
            p[3] = new SqlParameter("@PageSize", pageSize);

            string query = @"SELECT id, name, contactPerson, mobile, email, address, isActive, docId
             FROM CourierMaster
             WHERE (@OnlyActive = 0 OR isActive = 1)
             AND (@SearchText IS NULL OR name LIKE '%' + @SearchText + '%' OR contactPerson LIKE '%' + @SearchText + '%' OR mobile LIKE '%' + @SearchText + '%' OR email LIKE '%' + @SearchText + '%' OR address LIKE '%' + @SearchText + '%')
             ORDER BY name ASC
             OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];
            GetCourierListRes res = new GetCourierListRes();

            SqlParameter[] countP = new SqlParameter[2];
            countP[0] = new SqlParameter("@OnlyActive", req.OnlyActive);
            countP[1] = new SqlParameter("@SearchText", Utilities.DbValue(req.SearchText));

            string countQuery = @"SELECT COUNT(*)
                  FROM CourierMaster
                  WHERE (@OnlyActive = 0 OR isActive = 1)
                  AND (@SearchText IS NULL OR name LIKE '%' + @SearchText + '%' OR contactPerson LIKE '%' + @SearchText + '%' OR mobile LIKE '%' + @SearchText + '%' OR email LIKE '%' + @SearchText + '%' OR address LIKE '%' + @SearchText + '%')";

            DataTable countDt = _client.ExecuteAdapter(countQuery, countP, CommandType.Text).Tables[0];
            int totalCount = countDt.Rows[0].Field<int>(0);

            res.TotalCount = totalCount;
            res.PageNumber = pageNumber;
            res.PageSize = pageSize;

            SqlParameter[] metaP = new SqlParameter[4];
            metaP[0] = new SqlParameter("@OnlyActive", req.OnlyActive);
            metaP[1] = new SqlParameter("@SearchText", Utilities.DbValue(req.SearchText));
            metaP[2] = new SqlParameter("@Offset", offset);
            metaP[3] = new SqlParameter("@PageSize", pageSize);

            string metaQuery = @"SELECT m.courierMasterId, m.label, m.value, m.showInSlip
                 FROM CourierMetadata m
                 INNER JOIN CourierMaster cm ON cm.id = m.courierMasterId
                 WHERE cm.id IN (
                     SELECT id
                     FROM CourierMaster
                     WHERE (@OnlyActive = 0 OR isActive = 1)
                     AND (@SearchText IS NULL OR name LIKE '%' + @SearchText + '%' OR contactPerson LIKE '%' + @SearchText + '%' OR mobile LIKE '%' + @SearchText + '%' OR email LIKE '%' + @SearchText + '%' OR address LIKE '%' + @SearchText + '%')
                     ORDER BY name ASC
                     OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
                 )
                 ORDER BY cm.id ASC";

            DataTable metaDt = _client.ExecuteAdapter(metaQuery, metaP, CommandType.Text).Tables[0];

            Dictionary<int, List<CourierMetadataItem>> metaLookup = new Dictionary<int, List<CourierMetadataItem>>();

            foreach (DataRow mRow in metaDt.Rows)
            {
                int cmId = mRow.Field<int>("courierMasterId");

                if (!metaLookup.TryGetValue(cmId, out List<CourierMetadataItem>? mList))
                {
                    mList = new List<CourierMetadataItem>();
                    metaLookup[cmId] = mList;
                }

                mList.Add(new CourierMetadataItem
                {
                    CourierMasterId = cmId,
                    Label = mRow.Field<string>("label") ?? string.Empty,
                    Value = mRow.Field<string>("value") ?? string.Empty,
                    ShowInSlip = mRow.Field<bool>("showInSlip")
                });
            }

            foreach (DataRow row in dt.Rows)
            {
                int id = row.Field<int>("id");
                string? docId = row.Field<string?>("docId");
                string? docEdid = null;

                if (!string.IsNullOrWhiteSpace(docId))
                {
                    docEdid = EncryptionHelper.Encrypt(docId);
                }

                metaLookup.TryGetValue(id, out List<CourierMetadataItem>? courierMeta);

                res.Items.Add(new CourierListItem
                {
                    Id = id,
                    Edid = EncryptionHelper.Encrypt(id.ToString()),
                    Name = row.Field<string>("name") ?? string.Empty,
                    ContactPerson = row.Field<string?>("contactPerson"),
                    Mobile = row.Field<string?>("mobile"),
                    Email = row.Field<string?>("email"),
                    Address = row.Field<string?>("address"),
                    IsActive = row.Field<bool>("isActive"),
                    DocEdid = docEdid,
                    Metadata = courierMeta ?? new List<CourierMetadataItem>()
                });
            }

            return res;
        }

        public CourierListItem? GetById(int id)
        {
            SqlParameter[] p = { new SqlParameter("@Id", id) };

            string query = @"SELECT id, name, contactPerson, mobile, email, address, isActive, docId
                     FROM CourierMaster
                     WHERE id = @Id";

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];
            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            string? docId = row.Field<string?>("docId");
            string? docEdid = null;

            if (!string.IsNullOrWhiteSpace(docId))
            {
                docEdid = EncryptionHelper.Encrypt(docId);
            }

            List<CourierMetadataItem> metadata = GetMetadataByCourierId(id);

            return new CourierListItem
            {
                Id = id,
                Edid = EncryptionHelper.Encrypt(id.ToString()),
                Name = row.Field<string>("name") ?? string.Empty,
                ContactPerson = row.Field<string?>("contactPerson"),
                Mobile = row.Field<string?>("mobile"),
                Email = row.Field<string?>("email"),
                Address = row.Field<string?>("address"),
                IsActive = row.Field<bool>("isActive"),
                DocEdid = docEdid,
                Metadata = metadata
            };
        }
        
        public List<CourierMetadataItem> GetMetadataByCourierId(int courierId)
        {
            SqlParameter[] p = { new SqlParameter("@CourierId", courierId) };

            string query = @"SELECT courierMasterId, label, value, showInSlip
                     FROM CourierMetadata
                     WHERE courierMasterId = @CourierId";

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];
            List<CourierMetadataItem> list = new List<CourierMetadataItem>();

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new CourierMetadataItem
                {
                    CourierMasterId = row.Field<int>("courierMasterId"),
                    Label = row.Field<string>("label") ?? string.Empty,
                    Value = row.Field<string>("value") ?? string.Empty,
                    ShowInSlip = row.Field<bool>("showInSlip")
                });
            }

            return list;
        }

        public string Create(AddCourierReq req, int currentUserId = 1)
        {
            SqlParameter[] p = new SqlParameter[6];
            p[0] = new SqlParameter("@Name", req.Name.Trim());
            p[1] = new SqlParameter("@ContactPerson", Utilities.DbValue(req.ContactPerson));
            p[2] = new SqlParameter("@Mobile", Utilities.DbValue(req.Mobile));
            p[3] = new SqlParameter("@Email", Utilities.DbValue(req.Email));
            p[4] = new SqlParameter("@Address", Utilities.DbValue(req.Address));
            p[5] = new SqlParameter("@DocId", Utilities.DbValue(EncryptionHelper.Decrypt(req.DocEdid)));

            string query = @"INSERT INTO CourierMaster (name, contactPerson, mobile, email, address, isActive, docId, createdAt) 
                             OUTPUT INSERTED.id 
                             VALUES (@Name, @ContactPerson, @Mobile, @Email, @Address, 1, @DocId, SYSUTCDATETIME())";
            int newId = _client.ExecuteScalar<int>(query, p, CommandType.Text);

            if (req.Metadata != null && req.Metadata.Count > 0)
            {
                foreach (var meta in req.Metadata)
                {
                    if (string.IsNullOrWhiteSpace(meta.Label) || string.IsNullOrWhiteSpace(meta.Value))
                        continue;

                    SqlParameter[] mp = {
                        new SqlParameter("@CourierMasterId", newId),
                        new SqlParameter("@Label", meta.Label.Trim()),
                        new SqlParameter("@Value", meta.Value.Trim()),
                        new SqlParameter("@ShowInSlip", meta.ShowInSlip),
                        new SqlParameter("@CreatedBy", _currentUserService.EmployeeCode)
                    };
                    string mQuery = @"INSERT INTO CourierMetadata (courierMasterId, label, value, showInSlip, createdAt, createdBy) 
                                      VALUES (@CourierMasterId, @Label, @Value, @ShowInSlip, SYSUTCDATETIME(), @CreatedBy)";

                    _client.ExecuteNonQuery(mQuery, mp, CommandType.Text);
                }
            }

            return EncryptionHelper.Encrypt(newId.ToString());
        }

        public bool Update(UpdateCourierReq req)
        {
            int courierId = EncryptionHelper.Decrypt<int>(req.Edid);

            SqlParameter[] p = new SqlParameter[8];
            p[0] = new SqlParameter("@Id", courierId);
            p[1] = new SqlParameter("@Name", req.Name.Trim());
            p[2] = new SqlParameter("@ContactPerson", Utilities.DbValue(req.ContactPerson));
            p[3] = new SqlParameter("@Mobile", Utilities.DbValue(req.Mobile));
            p[4] = new SqlParameter("@Email", Utilities.DbValue(req.Email));
            p[5] = new SqlParameter("@Address", Utilities.DbValue(req.Address));
            p[6] = new SqlParameter("@DocId", Utilities.DbValue(req.DocId ?? req.DocEdid));
            p[7] = new SqlParameter("@IsActive", Utilities.DbValue<bool>(req.IsActive));

            string updateQuery = @"UPDATE CourierMaster  SET name = @Name, contactPerson = @ContactPerson,  mobile = @Mobile, email = @Email, 
                                   address = @Address, docId = @DocId,isActive = ISNULL(@IsActive, isActive) WHERE id = @Id";
            
            _client.ExecuteNonQuery(updateQuery, p, CommandType.Text);

            if (req.Metadata != null)
            {
                SqlParameter[] dp = { new SqlParameter("@CourierMasterId", courierId) };
                _client.ExecuteNonQuery("DELETE FROM CourierMetadata WHERE courierMasterId = @CourierMasterId", dp, CommandType.Text);

                foreach (var meta in req.Metadata)
                {
                    if (string.IsNullOrWhiteSpace(meta.Label) || string.IsNullOrWhiteSpace(meta.Value)) continue;

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

            return true;
        }
    }
}

