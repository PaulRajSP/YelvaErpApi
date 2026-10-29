using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Repository
{
    public interface IEmployeeRepository
    {
        EmployeeRecordDb? GetEmployeeRecordByCode(string employeeCode);
        List<string> GetEmployeeRoles(int employeeId);
        List<string> GetPermissionsForRoles(IEnumerable<string> roles);
        List<MenuItemDto> GetMenusForPermissions(IEnumerable<string> permissions);
        PageResult<EmployeeListItem> GetAllEmployees(GetEmployeeListReq req);
        EmployeeProfileRes? GetEmployeeById(int employeeId);
        int CreateEmployee(CreateEmployeeReq req, string passwordHash, string passwordSalt, string? createdBy = null);
        void UpdateEmployee(UpdateEmployeeReq req, string? passwordHash, string? passwordSalt);
        List<string> GetAvailableRoles();
        bool IsEmployeeCodeExists(string employeeCode, int? excludeId = null);
        string GenerateRandomEmployeeCode();
    }

    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly DbClient _client;

        public EmployeeRepository(DbClient client)
        {
            _client = client;
        }

        public EmployeeRecordDb? GetEmployeeRecordByCode(string employeeCode)
        {
            string query = @"
                SELECT em.employeeId, em.employeeCode, em.passwordHash, em.passwordSalt, em.isActive,
                       ISNULL(ead.employeeName, em.employeeCode) AS employeeName,
                       ead.email, ead.mobile, ead.documentIdJson
                FROM EmployeeMaster em
                LEFT JOIN EmployeeAdditionalDetails ead ON em.employeeId = ead.employeeMstId
                WHERE em.employeeCode = @EmployeeCode";

            SqlParameter[] p = new SqlParameter[]
            {
                new SqlParameter("@EmployeeCode", employeeCode.Trim())
            };

            DataSet ds = _client.ExecuteAdapter(query, p, CommandType.Text);
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                DataRow row = ds.Tables[0].Rows[0];
                return new EmployeeRecordDb
                {
                    EmployeeId = Convert.ToInt32(row["employeeId"]),
                    EmployeeCode = row.Field<string>("employeeCode") ?? string.Empty,
                    PasswordHash = row.Field<string>("passwordHash") ?? string.Empty,
                    PasswordSalt = row.Field<string>("passwordSalt") ?? string.Empty,
                    IsActive = Convert.ToBoolean(row["isActive"]),
                    EmployeeName = row.Field<string>("employeeName") ?? string.Empty,
                    Email = row.Field<string?>("email"),
                    Mobile = row.Field<string?>("mobile"),
                    DocumentIdJson = row.Field<string?>("documentIdJson")
                };
            }

            return null;
        }

        public List<string> GetEmployeeRoles(int employeeId)
        {
            string query = "SELECT role FROM EmployeePermittedRole WHERE employeeMstId = @EmployeeId";
            SqlParameter[] p = new SqlParameter[]
            {
                new SqlParameter("@EmployeeId", employeeId)
            };

            DataSet ds = _client.ExecuteAdapter(query, p, CommandType.Text);
            List<string> roles = new List<string>();

            if (ds != null && ds.Tables.Count > 0)
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    string? role = row.Field<string>("role");
                    if (!string.IsNullOrWhiteSpace(role))
                    {
                        roles.Add(role.Trim());
                    }
                }
            }

            return roles;
        }


        public List<string> GetPermissionsForRoles(IEnumerable<string> roles)
        {
            List<string> roleList = roles?.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>();

            if (roleList.Count == 0) return new List<string>();

            List<SqlParameter> parameters = new List<SqlParameter>();
            List<string> paramPlaceholders = new List<string>();

            for (int i = 0; i < roleList.Count; i++)
            {
                string paramName = $"@Role{i}";
                paramPlaceholders.Add(paramName);
                parameters.Add(new SqlParameter(paramName, roleList[i]));
            }

            string query = $@" SELECT DISTINCT permissionEnum FROM PermissionMaster WHERE LTRIM(RTRIM(role)) IN ({string.Join(",", paramPlaceholders)}) AND permissionEnum IS NOT NULL";

            DataSet ds = _client.ExecuteAdapter(query, parameters.ToArray(), CommandType.Text);
            List<string> permissions = new List<string>();

            if (ds != null && ds.Tables.Count > 0)
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    string? perm = row.Field<string>("permissionEnum");

                    if (!string.IsNullOrWhiteSpace(perm))
                    {
                        permissions.Add(perm.Trim());
                    }
                }
            }

            return permissions;
        }


        public List<MenuItemDto> GetMenusForPermissions(IEnumerable<string> permissions)
        {
            List<string> permList = permissions?.Where(p => !string.IsNullOrWhiteSpace(p)).ToList() ?? new List<string>();
            if (permList.Count == 0) return new List<MenuItemDto>();

            List<SqlParameter> parameters = new List<SqlParameter>();
            List<string> paramPlaceholders = new List<string>();

            for (int i = 0; i < permList.Count; i++)
            {
                string paramName = $"@Perm{i}";
                paramPlaceholders.Add(paramName);
                parameters.Add(new SqlParameter(paramName, permList[i]));
            }

            string query = $@"
                SELECT id, moduleName, menuName, permissionEnum, frontendUrl, isActive
                FROM MenuMaster
                WHERE isActive = 1 AND permissionEnum IN ({string.Join(",", paramPlaceholders)})
                ORDER BY id ASC";

            DataSet ds = _client.ExecuteAdapter(query, parameters.ToArray(), CommandType.Text);
            List<MenuItemDto> menus = new List<MenuItemDto>();

            if (ds != null && ds.Tables.Count > 0)
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    menus.Add(new MenuItemDto
                    {
                        Id = Convert.ToInt32(row["id"]),
                        ModuleName = row.Field<string>("moduleName") ?? string.Empty,
                        MenuName = row.Field<string>("menuName") ?? string.Empty,
                        PermissionEnum = row.Field<string>("permissionEnum") ?? string.Empty,
                        FrontendUrl = row.Field<string>("frontendUrl") ?? string.Empty,
                        IsActive = Convert.ToBoolean(row["isActive"])
                    });
                }
            }

            return menus;
        }

        public PageResult<EmployeeListItem> GetAllEmployees(GetEmployeeListReq req)
        {
            int pageNumber = req.Pagination.PageNumber > 0 ? req.Pagination.PageNumber : 1;
            int pageSize = req.Pagination.PageSize > 0 ? req.Pagination.PageSize : 10;
            int offset = (pageNumber - 1) * pageSize;

            string? search = string.IsNullOrWhiteSpace(req.SearchTerm) ? null : $"%{req.SearchTerm.Trim()}%";

            string query = @"
                SELECT COUNT(*) OVER() AS TotalRecords,
                       em.employeeId, em.employeeCode, em.isActive,
                       em.createdBy, em.createdAt,
                       ISNULL(ead.employeeName, '') AS employeeName,
                       ead.email, ead.mobile, ead.documentIdJson,
                       (
                           SELECT STRING_AGG(epr.role, ',')
                           FROM EmployeePermittedRole epr
                           WHERE epr.employeeMstId = em.employeeId
                       ) AS RolesCombined
                FROM EmployeeMaster em
                LEFT JOIN EmployeeAdditionalDetails ead ON em.employeeId = ead.employeeMstId
                WHERE (@Search IS NULL OR em.employeeCode LIKE @Search 
                                       OR ead.employeeName LIKE @Search 
                                       OR ead.email LIKE @Search 
                                       OR ead.mobile LIKE @Search)
                  AND (@IsActive IS NULL OR em.isActive = @IsActive)
                  AND (@Role IS NULL OR EXISTS (
                          SELECT 1 FROM EmployeePermittedRole r 
                          WHERE r.employeeMstId = em.employeeId AND r.role = @Role
                      ))
                ORDER BY em.employeeId DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            SqlParameter[] p = new SqlParameter[]
            {
                new SqlParameter("@Search", (object?)search ?? DBNull.Value),
                new SqlParameter("@IsActive", (object?)req.IsActive ?? DBNull.Value),
                new SqlParameter("@Role", string.IsNullOrWhiteSpace(req.Role) ? DBNull.Value : (object)req.Role.Trim()),
                new SqlParameter("@Offset", offset),
                new SqlParameter("@PageSize", pageSize)
            };

            DataSet ds = _client.ExecuteAdapter(query, p, CommandType.Text);

            PageResult<EmployeeListItem> result = new PageResult<EmployeeListItem>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = 0
            };

            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                result.TotalCount = Convert.ToInt32(ds.Tables[0].Rows[0]["TotalRecords"]);

                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    string? rolesStr = row.Field<string?>("RolesCombined");
                    List<string> roles = string.IsNullOrWhiteSpace(rolesStr)
                        ? new List<string>()
                        : rolesStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

                    result.Items.Add(new EmployeeListItem
                    {
                        EmployeeId = Convert.ToInt32(row["employeeId"]),
                        EmployeeCode = row.Field<string>("employeeCode") ?? string.Empty,
                        EmployeeName = row.Field<string>("employeeName") ?? string.Empty,
                        Email = row.Field<string?>("email"),
                        Mobile = row.Field<string?>("mobile"),
                        DocumentIdJson = row.Field<string?>("documentIdJson"),
                        IsActive = Convert.ToBoolean(row["isActive"]),
                        Roles = roles,
                        CreatedBy = row.Table.Columns.Contains("createdBy") && row["createdBy"] != DBNull.Value ? row.Field<string?>("createdBy") : null,
                        CreatedAt = row.Table.Columns.Contains("createdAt") && row["createdAt"] != DBNull.Value ? row.Field<DateTime?>("createdAt") : null
                    });
                }
            }

            return result;
        }

        public EmployeeProfileRes? GetEmployeeById(int employeeId)
        {
            string query = @"
                SELECT em.employeeId, em.employeeCode, em.isActive,
                       em.createdBy, em.createdAt,
                       ISNULL(ead.employeeName, '') AS employeeName,
                       ead.email, ead.mobile, ead.documentIdJson
                FROM EmployeeMaster em
                LEFT JOIN EmployeeAdditionalDetails ead ON em.employeeId = ead.employeeMstId
                WHERE em.employeeId = @EmployeeId";

            SqlParameter[] p = new SqlParameter[]
            {
                new SqlParameter("@EmployeeId", employeeId)
            };

            DataSet ds = _client.ExecuteAdapter(query, p, CommandType.Text);
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                DataRow row = ds.Tables[0].Rows[0];
                List<string> roles = GetEmployeeRoles(employeeId);

                return new EmployeeProfileRes
                {
                    EmployeeId = Convert.ToInt32(row["employeeId"]),
                    EmployeeCode = row.Field<string>("employeeCode") ?? string.Empty,
                    EmployeeName = row.Field<string>("employeeName") ?? string.Empty,
                    Email = row.Field<string?>("email"),
                    Mobile = row.Field<string?>("mobile"),
                    DocumentIdJson = row.Field<string?>("documentIdJson"),
                    IsActive = Convert.ToBoolean(row["isActive"]),
                    Roles = roles,
                    CreatedBy = row.Table.Columns.Contains("createdBy") && row["createdBy"] != DBNull.Value ? row.Field<string?>("createdBy") : null,
                    CreatedAt = row.Table.Columns.Contains("createdAt") && row["createdAt"] != DBNull.Value ? row.Field<DateTime?>("createdAt") : null
                };
            }

            return null;
        }

        public int CreateEmployee(CreateEmployeeReq req, string passwordHash, string passwordSalt, string? createdBy = null)
        {
            // Insert EmployeeMaster
            string insertEmpQuery = @"
                INSERT INTO EmployeeMaster (employeeCode, passwordHash, passwordSalt, isActive, createdBy, createdAt)
                VALUES (@EmployeeCode, @PasswordHash, @PasswordSalt, 1, @CreatedBy, GETUTCDATE());
                SELECT SCOPE_IDENTITY();";

            SqlParameter[] pEmp = new SqlParameter[]
            {
                new SqlParameter("@EmployeeCode", req.EmployeeCode.Trim()),
                new SqlParameter("@PasswordHash", passwordHash),
                new SqlParameter("@PasswordSalt", passwordSalt),
                new SqlParameter("@CreatedBy", Utilities.DbValue(createdBy))
            };

            DataSet ds = _client.ExecuteAdapter(insertEmpQuery, pEmp, CommandType.Text);
            int employeeId = Convert.ToInt32(ds.Tables[0].Rows[0][0]);

            // Insert EmployeeAdditionalDetails
            string insertDetailsQuery = @"
                INSERT INTO EmployeeAdditionalDetails (employeeMstId, employeeName, email, mobile, documentIdJson)
                VALUES (@EmployeeMstId, @EmployeeName, @Email, @Mobile, @DocumentIdJson);";

            SqlParameter[] pDetails = new SqlParameter[]
            {
                new SqlParameter("@EmployeeMstId", employeeId),
                new SqlParameter("@EmployeeName", req.EmployeeName.Trim()),
                new SqlParameter("@Email", Utilities.DbValue(req.Email)),
                new SqlParameter("@Mobile", Utilities.DbValue(req.Mobile)),
                new SqlParameter("@DocumentIdJson", Utilities.DbValue(req.DocumentIdJson))
            };

            _client.ExecuteAdapter(insertDetailsQuery, pDetails, CommandType.Text);

            // Insert Roles
            if (req.Roles != null && req.Roles.Count > 0)
            {
                foreach (var role in req.Roles.Distinct())
                {
                    if (string.IsNullOrWhiteSpace(role)) continue;
                    string insertRoleQuery = @"
                        INSERT INTO EmployeePermittedRole (employeeMstId, role)
                        VALUES (@EmployeeMstId, @Role);";

                    string normalizedRole = Enum.TryParse<RoleEnum>(role.Trim(), true, out var parsedRole)
                        ? parsedRole.ToString()
                        : role.Trim();

                    SqlParameter[] pRole = new SqlParameter[]
                    {
                        new SqlParameter("@EmployeeMstId", employeeId),
                        new SqlParameter("@Role", normalizedRole)
                    };

                    _client.ExecuteAdapter(insertRoleQuery, pRole, CommandType.Text);
                }
            }

            return employeeId;
        }

        public void UpdateEmployee(UpdateEmployeeReq req, string? passwordHash, string? passwordSalt)
        {
            // Update EmployeeMaster
            string updateEmpQuery = @"
                UPDATE EmployeeMaster
                SET isActive = @IsActive
                    " + (!string.IsNullOrWhiteSpace(passwordHash) ? ", passwordHash = @PasswordHash, passwordSalt = @PasswordSalt " : "") + @"
                WHERE employeeId = @EmployeeId;";

            List<SqlParameter> pEmp = new List<SqlParameter>
            {
                new SqlParameter("@EmployeeId", req.EmployeeId),
                new SqlParameter("@IsActive", req.IsActive)
            };

            if (!string.IsNullOrWhiteSpace(passwordHash) && !string.IsNullOrWhiteSpace(passwordSalt))
            {
                pEmp.Add(new SqlParameter("@PasswordHash", passwordHash));
                pEmp.Add(new SqlParameter("@PasswordSalt", passwordSalt));
            }

            _client.ExecuteAdapter(updateEmpQuery, pEmp.ToArray(), CommandType.Text);

            // Update EmployeeAdditionalDetails (Upsert pattern)
            string upsertDetailsQuery = @"
                IF EXISTS (SELECT 1 FROM EmployeeAdditionalDetails WHERE employeeMstId = @EmployeeId)
                BEGIN
                    UPDATE EmployeeAdditionalDetails
                    SET employeeName = @EmployeeName,
                        email = @Email,
                        mobile = @Mobile,
                        documentIdJson = @DocumentIdJson
                    WHERE employeeMstId = @EmployeeId;
                END
                ELSE
                BEGIN
                    INSERT INTO EmployeeAdditionalDetails (employeeMstId, employeeName, email, mobile, documentIdJson)
                    VALUES (@EmployeeId, @EmployeeName, @Email, @Mobile, @DocumentIdJson);
                END";

            SqlParameter[] pDetails = new SqlParameter[]
            {
                new SqlParameter("@EmployeeId", req.EmployeeId),
                new SqlParameter("@EmployeeName", req.EmployeeName.Trim()),
                new SqlParameter("@Email", Utilities.DbValue(req.Email)),
                new SqlParameter("@Mobile", Utilities.DbValue(req.Mobile)),
                new SqlParameter("@DocumentIdJson", Utilities.DbValue(req.DocumentIdJson))
            };

            _client.ExecuteAdapter(upsertDetailsQuery, pDetails, CommandType.Text);

            // Update Roles: Replace existing
            string deleteRolesQuery = "DELETE FROM EmployeePermittedRole WHERE employeeMstId = @EmployeeId;";
            _client.ExecuteAdapter(deleteRolesQuery, new SqlParameter[] { new SqlParameter("@EmployeeId", req.EmployeeId) }, CommandType.Text);

            if (req.Roles != null && req.Roles.Count > 0)
            {
                foreach (var role in req.Roles.Distinct())
                {
                    if (string.IsNullOrWhiteSpace(role)) continue;
                    string insertRoleQuery = @"
                        INSERT INTO EmployeePermittedRole (employeeMstId, role)
                        VALUES (@EmployeeId, @Role);";

                    string normalizedRole = Enum.TryParse<RoleEnum>(role.Trim(), true, out var parsedRole)
                        ? parsedRole.ToString()
                        : role.Trim();

                    SqlParameter[] pRole = new SqlParameter[]
                    {
                        new SqlParameter("@EmployeeId", req.EmployeeId),
                        new SqlParameter("@Role", normalizedRole)
                    };

                    _client.ExecuteAdapter(insertRoleQuery, pRole, CommandType.Text);
                }
            }
        }

        public List<string> GetAvailableRoles()
        {
            string query = "SELECT DISTINCT role FROM PermissionMaster WHERE role IS NOT NULL AND role <> '' ORDER BY role ASC";
            DataSet ds = _client.ExecuteAdapter(query, null, CommandType.Text);
            List<string> roles = new List<string>();

            if (ds != null && ds.Tables.Count > 0)
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    string? role = row.Field<string>("role");
                    if (!string.IsNullOrWhiteSpace(role))
                    {
                        roles.Add(role.Trim());
                    }
                }
            }

            // Ensure all enum roles are present
            foreach (var enumRole in Enum.GetNames<RoleEnum>())
            {
                if (!roles.Any(r => r.Equals(enumRole, StringComparison.OrdinalIgnoreCase)))
                {
                    roles.Add(enumRole);
                }
            }

            return roles;
        }

        public bool IsEmployeeCodeExists(string employeeCode, int? excludeId = null)
        {
            string query = @"
                SELECT 1 FROM EmployeeMaster 
                WHERE employeeCode = @EmployeeCode 
                  AND (@ExcludeId IS NULL OR employeeId <> @ExcludeId)";

            SqlParameter[] p = new SqlParameter[]
            {
                new SqlParameter("@EmployeeCode", employeeCode.Trim()),
                new SqlParameter("@ExcludeId", (object?)excludeId ?? DBNull.Value)
            };

            DataSet ds = _client.ExecuteAdapter(query, p, CommandType.Text);
            return ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0;
        }

        public string GenerateRandomEmployeeCode()
        {
            string candidate;
            do
            {
                candidate = Utilities.GeneratePublicId("EMP", 4);
            } while (IsEmployeeCodeExists(candidate));

            return candidate;
        }
    }
}
