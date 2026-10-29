using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Services;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
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
        List<AttachmentModel> GetModuleAttachments(string moduleName, string recordEdid);
        bool DeactivateModuleAttachment(int moduleAttachmentId);
        bool DeactivateModuleAttachments(string moduleName, int recordId, string? attachmentType = null);
    }

    public class CommonRepository : ICommonRepository
    {
        private readonly DbClient _client;
        private readonly ICurrentUserService _currentUserService;

        public CommonRepository(DbClient client, ICurrentUserService currentUserService)
        {
            _client = client;
            _currentUserService = currentUserService;
        }

        public EnumListResponse GetEnumValues(string typeName)
        {
            EnumListResponse res = new EnumListResponse();

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
            int attachmentId;
            if (!int.TryParse(edid, out attachmentId))
            {
                attachmentId = EncryptionHelper.Decrypt<int>(edid);
            }

            Microsoft.Data.SqlClient.SqlParameter[] p = new Microsoft.Data.SqlClient.SqlParameter[1];
            p[0] = new Microsoft.Data.SqlClient.SqlParameter("@Id", attachmentId);

            string query = "SELECT originalName, storedName, filePath FROM AttachmentMaster WHERE id=@Id";
            System.Data.DataTable dt = _client.ExecuteAdapter(query, p, System.Data.CommandType.Text).Tables[0];

            if (dt.Rows.Count > 0)
            {
                return new AttachmentModel
                {
                    OriginalName = dt.Rows[0]["originalName"]?.ToString() ?? "",
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

                parameters.Add(new SqlParameter("@ModuleName" + i, item.ModuleName));
                parameters.Add(new SqlParameter("@RecordId" + i, EncryptionHelper.Decrypt<int>(item.RecordEdid)));
                parameters.Add(new SqlParameter("@AttachmentType" + i, Utilities.DbValue(item.AttachmentType)));
                parameters.Add(new SqlParameter("@AttachmentId" + i, EncryptionHelper.Decrypt<int>(item.AttachmentEdid)));
                parameters.Add(new SqlParameter("@Remarks" + i, Utilities.DbValue(item.Remarks)));
                parameters.Add(new SqlParameter("@CreatedBy" + i, _currentUserService.EmployeeCode));

                values.Add("(@ModuleName" + i + ", @RecordId" + i + ", @AttachmentType" + i + ", @AttachmentId" + i + ", @Remarks" + i + ", @CreatedBy" + i + ")");
        }

            string query = @"INSERT INTO ModuleAttachment(ModuleName, RecordId, AttachmentType, AttachmentId, Remarks, CreatedBy)
                             VALUES " + string.Join(", ", values);

            return _client.ExecuteNonQuery(query, parameters.ToArray(), CommandType.Text) > 0;
        }
    }
}
