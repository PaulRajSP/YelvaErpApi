using AttachmentEnum;
using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Linq;
using WebApplication1.Common;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Repository
{
    public interface ICommonRepository
    {
        EnumListResponse GetEnumValues(string typeName);
        int InsertAttachment(AttachmentModel model);
        AttachmentModel? GetAttachment(string edid, string fileName);
        bool AddModuleAttachment(List<AttachmentModuleReq> req);
        bool ClearModuleAttachments(string moduleName, string recordEdid);
        List<AttachmentModuleRes> GetModuleAttachments(GetModuleAttachmentReq req);
    }

    public class CommonRepository : ICommonRepository
    {
        private readonly DbClient _client;
        private readonly CurrentUserService _currentUserService;

        public CommonRepository(DbClient client, CurrentUserService currentUserService)
        {
            _client = client;
            _currentUserService = currentUserService;
        }

        public EnumListResponse GetEnumValues(string typeName)
        {
            var res = new EnumListResponse();

            switch (typeName)
            {
                case "SupplierType":
                    res.Items = Enum.GetNames(typeof(SupplierType)).ToList();
                    break;
                case "customerType":
                    res.Items = Enum.GetNames(typeof(CustomerType)).ToList();
                    break;
                case "PurchaseStatus":
                    res.Items = Enum.GetNames(typeof(PurchaseStatus)).Select(x => x.ToUpper()).ToList();
                    break;
                case "PaymentType":
                    res.Items = Enum.GetNames(typeof(PaymentType)).ToList();
                    break;
                case "StockStatus":
                    res.Items = Enum.GetNames(typeof(StockStatus)).ToList();
                    break;
                case "ProductionStatus":
                    res.Items = Enum.GetNames(typeof(ProductionStatus)).ToList();
                    break;
                case "ProductionPriority":
                    res.Items = Enum.GetNames(typeof(ProductionPriority)).ToList();
                    break;
                case "OrderStatus":
                    res.Items = Enum.GetNames(typeof(OrderStatus)).ToList();
                    break;
                case "PaymentStatus":
                    res.Items = Enum.GetNames(typeof(PaymentStatus)).ToList();
                    break;
                case "OrderProductItemStatus":
                    res.Items = Enum.GetNames(typeof(OrderProductItemStatus)).ToList();
                    break;
                case "PackageStatus":
                    res.Items = Enum.GetNames(typeof(PackageStatus)).ToList();
                    break;
                case "ExpenseCategory":
                    res.Items = Enum.GetNames(typeof(ExpenseCategory)).ToList();
                    break;
                case "ExpenseStatus":
                    res.Items = Enum.GetNames(typeof(ExpenseStatus)).ToList();
                    break;
                case "ExpensePaymentMode":
                    res.Items = Enum.GetNames(typeof(ExpensePaymentMode)).ToList();
                    break;
                case "TransactionCategory":
                case "PaymentTransactionCategory":
                    res.Items = Enum.GetNames(typeof(TransactionCategory)).ToList();
                    break;
                case "EmployeeDocument":
                    res.Items = Enum.GetNames(typeof(EmployeeDocumentEnum)).ToList();
                    break;
                case "CourierAttachmentType":
                    res.Items = Enum.GetNames(typeof(CourierAttachmentType)).ToList();
                    break;

                default:
                    throw new HoneyErpException(ErrorCode.InvalidData, $"Enum type '{typeName}' is not supported or not found.");
            }

            return res;
        }

        public int InsertAttachment(AttachmentModel model)
        {
            Microsoft.Data.SqlClient.SqlParameter[] p = new Microsoft.Data.SqlClient.SqlParameter[3];
            p[0] = new Microsoft.Data.SqlClient.SqlParameter("@OriginalName", model.OriginalName);
            p[1] = new Microsoft.Data.SqlClient.SqlParameter("@StoredName", model.StoredName);
            p[2] = new Microsoft.Data.SqlClient.SqlParameter("@FilePath", model.FilePath);

            string query = "INSERT INTO AttachmentMaster (originalName, storedName, filePath) OUTPUT INSERTED.id VALUES (@OriginalName, @StoredName, @FilePath)";
            return _client.ExecuteScalar<int>(query, p, System.Data.CommandType.Text);
        }

        public AttachmentModel? GetAttachment(string edid, string? fileName = null)
        {
            if (string.IsNullOrWhiteSpace(edid))
                return null;

            int attachmentId = EncryptionHelper.Decrypt<int>(edid);

            Microsoft.Data.SqlClient.SqlParameter[] p = new Microsoft.Data.SqlClient.SqlParameter[2];
            p[0] = new Microsoft.Data.SqlClient.SqlParameter("@Id", attachmentId);
            p[1] = new Microsoft.Data.SqlClient.SqlParameter("@OriginalName", string.IsNullOrWhiteSpace(fileName) ? DBNull.Value : fileName);

            string query = @"SELECT originalName, storedName, filePath
                             FROM AttachmentMaster
                             WHERE id = @Id AND (@OriginalName IS NULL OR originalName = @OriginalName)";
            System.Data.DataTable dt = _client.ExecuteAdapter(query, p, System.Data.CommandType.Text).Tables[0];

            if (dt.Rows.Count > 0)
            {
                string originalName = dt.Rows[0]["originalName"]?.ToString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(fileName) && !string.Equals(originalName, fileName, StringComparison.Ordinal))
                    return null;

                return new AttachmentModel
                {
                    OriginalName = originalName,
                    StoredName = dt.Rows[0]["storedName"]?.ToString() ?? "",
                    FilePath = dt.Rows[0]["filePath"]?.ToString() ?? ""
                };
            }

            return null;
        }

        public bool AddModuleAttachment(List<AttachmentModuleReq> req)
        {
            if (req == null || req.Count == 0)
            {
                return false;
            }

            List<SqlParameter> parameters = new List<SqlParameter>();
            List<string> values = new List<string>();

            for (int i = 0; i < req.Count; i++)
            {
                AttachmentModuleReq item = req[i];

                parameters.Add(new SqlParameter("@ModuleName" + i, item.ModuleName.HasValue ? item.ModuleName.Value.ToString() : "Courier"));
                parameters.Add(new SqlParameter("@RecordId" + i, EncryptionHelper.Decrypt<int>(item.RecordEdid)));
                parameters.Add(new SqlParameter("@AttachmentType" + i, Utilities.DbValue(item.AttachmentType)));
                parameters.Add(new SqlParameter("@AttachmentId" + i, EncryptionHelper.Decrypt<int>(item.AttachmentEdid)));
                parameters.Add(new SqlParameter("@Remarks" + i, Utilities.DbValue(item.Remarks)));
                parameters.Add(new SqlParameter("@CreatedBy" + i, _currentUserService.EmployeeCode));

                values.Add("(@ModuleName" + i + ", @RecordId" + i + ", @AttachmentType" + i + ", @AttachmentId" + i + ", @Remarks" + i + ", @CreatedBy" + i + ")");
            }

            string query = @"BEGIN TRY
                        BEGIN TRANSACTION;

                        UPDATE ModuleAttachment SET IsActive = 0 WHERE ModuleName = @ModuleName0 AND RecordId = @RecordId0;

                        MERGE ModuleAttachment AS target
                        USING (VALUES " + string.Join(", ", values) + @") AS source (ModuleName, RecordId, AttachmentType, AttachmentId, Remarks, CreatedBy)
                        ON target.ModuleName = source.ModuleName
                        AND target.RecordId = source.RecordId
                        AND target.AttachmentId = source.AttachmentId
                        WHEN MATCHED THEN UPDATE SET target.AttachmentType = source.AttachmentType, target.Remarks = source.Remarks, target.CreatedBy = source.CreatedBy, target.IsActive = 1
                        WHEN NOT MATCHED BY TARGET THEN INSERT (ModuleName, RecordId, AttachmentType, AttachmentId, Remarks, CreatedBy, IsActive)
                        VALUES (source.ModuleName, source.RecordId, source.AttachmentType, source.AttachmentId, source.Remarks, source.CreatedBy, 1);

                        COMMIT TRANSACTION;
                    END TRY
                    BEGIN CATCH
                        IF @@TRANCOUNT > 0
                            ROLLBACK TRANSACTION;

                        THROW;
                    END CATCH;";

            return _client.ExecuteNonQuery(query, parameters.ToArray(), CommandType.Text) >= 0;
        }

        public bool ClearModuleAttachments(string moduleName, string recordEdid)
        {
            SqlParameter[] p = new SqlParameter[2];
            p[0] = new SqlParameter("@ModuleName", moduleName);
            p[1] = new SqlParameter("@RecordId", EncryptionHelper.Decrypt<int>(recordEdid));

            string query = "UPDATE ModuleAttachment SET IsActive = 0 WHERE ModuleName = @ModuleName AND RecordId = @RecordId;";
            return _client.ExecuteNonQuery(query, p, CommandType.Text) >= 0;
        }

        public List<AttachmentModuleRes> GetModuleAttachments(GetModuleAttachmentReq req)
        {
            SqlParameter[] p = new SqlParameter[2];
            p[0] = new SqlParameter("@ModuleName", req.ModuleName);
            p[1] = new SqlParameter("@RecordId", EncryptionHelper.Decrypt<int>(req.RecordEdid));

            string query = @"SELECT a.Id AS AttachmentId, a.OriginalName, ma.AttachmentType, ma.Remarks FROM ModuleAttachment ma
                     INNER JOIN AttachmentMaster a ON a.Id = ma.AttachmentId WHERE ma.ModuleName = @ModuleName
                     AND ma.RecordId = @RecordId AND ma.IsActive = 1 ORDER BY ma.Id DESC";

            DataTable dt = _client.ExecuteAdapter(query, p, CommandType.Text).Tables[0];

            List<AttachmentModuleRes> result = new List<AttachmentModuleRes>();

            foreach (DataRow row in dt.Rows)
            {
                AttachmentModuleRes document = new AttachmentModuleRes();
                document.AttachmentEdid = EncryptionHelper.Encrypt(row.Field<int>("AttachmentId").ToString());
                document.AttachmentOriginalName = row.Field<string>("OriginalName") ?? "";
                document.AttachmentType = row.Field<string>("AttachmentType");
                document.Remarks = row.Field<string?>("Remarks");

                result.Add(document);
            }

            return result;
        }
    }
}
