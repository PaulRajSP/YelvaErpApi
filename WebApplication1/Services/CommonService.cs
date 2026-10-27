using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

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

        public async Task<AddAttachmentRes> SaveAttachment(IFormFile file)
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

            return new AddAttachmentRes
            {
                Edid = EncryptionHelper.Encrypt(attachmentId.ToString())
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
    }
}
