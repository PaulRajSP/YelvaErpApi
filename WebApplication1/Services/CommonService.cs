using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using AttachmentEnum;
using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Services
{
    public class CommonService
    {
        private readonly ICommonRepository _repo;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CommonService> _logger;

        public CommonService(
            ICommonRepository repo,
            IConfiguration config,
            IHttpClientFactory httpClientFactory,
            ILogger<CommonService> logger)
        {
            _repo = repo;
            _config = config;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public EnumListResponse GetTypes(string typeName)
        {
            return _repo.GetEnumValues(typeName);
        }

        public async Task<AttachmentModuleRes> SaveAttachment(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new HoneyErpException(ErrorCode.Failed,"No file uploaded or file is empty.");
            }

            string originalName = Path.GetFileName(file.FileName);
            string extension = Path.GetExtension(originalName);
            string storedName = $"{Guid.NewGuid():N}{extension}";

            string connectionString = _config["AzureBlobStorage:ConnectionString"]
                ?? throw new InvalidOperationException("Azure Blob Storage connection string is missing.");

            string containerName = _config["AzureBlobStorage:ContainerName"]
                ?? throw new InvalidOperationException("Azure Blob Storage container name is missing.");

            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            await containerClient.CreateIfNotExistsAsync();

            BlobClient blobClient = containerClient.GetBlobClient(storedName);

            BlobUploadOptions uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = GetContentType(extension)
                }
            };

            using (Stream stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, uploadOptions);
            }

            AttachmentModel model = new AttachmentModel
            {
                OriginalName = originalName,
                StoredName = storedName,
                FilePath = blobClient.Uri.ToString()
            };

            int attachmentId = _repo.InsertAttachment(model);

            string edid = EncryptionHelper.Encrypt(attachmentId.ToString());
            return new AttachmentModuleRes
            {
                AttachmentEdid = edid,
                AttachmentOriginalName = originalName
            };
        }

        public AttachmentModel GetAttachment(string edid,string fileName)
        {
            var model = _repo.GetAttachment(edid, fileName);
            if (model == null || string.IsNullOrEmpty(model.FilePath))
                throw new HoneyErpException(ErrorCode.NotFound, "Attachment not found.");
            return model;
        }

        public static string GetContentType(string ext)
        {
            return ext.ToLowerInvariant() switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".csv" => "text/csv",
                ".json" => "application/json",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };
        }

        public bool AddModuleAttachment(List<AttachmentModuleReq> req, bool isMutiAttachemtn)
        {
            if (req == null || req.Count == 0)
            {
                return false;
            }

            if (!isMutiAttachemtn)
            {
                req = req.Take(1).ToList();
            }

            return _repo.AddModuleAttachment(req);
        }

        public bool SetModuleAttachments(List<AttachmentModuleReq> req)
        {
            if (req.Count > 0)
                return _repo.AddModuleAttachment(req);

            return true;
        }

        public bool ClearModuleAttachments(string moduleName, string recordEdid)
        {
            return _repo.ClearModuleAttachments(moduleName, recordEdid);

            return true;
        }

        public bool SetModuleAttachments(List<DocumentAttachmentReq>? attachments, AttachmentModule moduleName, string recordEdid)
        {
            if (attachments == null || attachments.Count == 0)
                return true;

            List<AttachmentModuleReq> requests = attachments
                .Where(item => !string.IsNullOrWhiteSpace(item.AttachmentEdid))
                .Select(item => new AttachmentModuleReq
                {
                    AttachmentEdid = item.AttachmentEdid,
                    RecordEdid = recordEdid,
                    ModuleName = moduleName,
                    AttachmentType = item.AttachmentType ?? "Document",
                    Remarks = item.Remarks ?? string.Empty
                })
                .ToList();

            return requests.Count == 0 || _repo.AddModuleAttachment(requests);
        }

        public List<AttachmentModuleRes> GetModuleAttachments(AttachmentModule moduleName, string recordEdid)
        {
            return _repo.GetModuleAttachments(new GetModuleAttachmentReq
            {
                ModuleName = moduleName.ToString(),
                RecordEdid = recordEdid
            });
        }

        public ListResult<AttachmentModuleRes> GetModuleAttachments(GetModuleAttachmentReq req)
        {
            if (string.IsNullOrWhiteSpace(req.ModuleName))
                throw new HoneyErpException(ErrorCode.ValidationFailed, "Module name is required.");

            if (string.IsNullOrWhiteSpace(req.RecordEdid))
                throw new HoneyErpException(ErrorCode.ValidationFailed, "Record EDID is required.");

            ListResult<AttachmentModuleRes> res = new ListResult<AttachmentModuleRes>();
            res.Items = _repo.GetModuleAttachments(req);

            return res;
        }
    }
}
