using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace HoneyErpAPI.Controllers
{
    [ApiController]
    [Route("api/common")]
    public class CommonController : ControllerBase
    {
        private readonly CommonService _service;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public CommonController(
            CommonService service,
            IHttpClientFactory httpClientFactory,
            IConfiguration config)
        {
            _service = service;
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        [HttpGet("type/{typeName}")]
        public IActionResult GetTypes(string typeName)
        {
            EnumListResponse data = _service.GetTypes(typeName);
            return Ok(data);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile? file)
        {
            ApiResponseModel<DocumentModel> res = new ApiResponseModel<DocumentModel>();
            res.Success = true;
            res.Data= await _service.SaveAttachment(file);

            return Ok(res);
        }

        [HttpGet("attachment/{edid}/{fileName}")]
        public async Task<IActionResult> GetAttachment(string edid, string fileName)
        {
            if (string.IsNullOrWhiteSpace(edid))
                return BadRequest("Attachment identifier is required.");

            try
            {
                AttachmentModel? attachment = _service.GetAttachment(edid, fileName);

                if (attachment == null || string.IsNullOrEmpty(attachment.FilePath))
                    return NotFound("Attachment not found.");

                string ext = Path.GetExtension(attachment.OriginalName ?? attachment.FilePath).ToLowerInvariant();
                string contentType = CommonService.GetContentType(ext);

                // Azure Blob Storage or another remote URL
                if (attachment.FilePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    attachment.FilePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    string? blobServiceUri = _config["AzureBlobStorage:BlobServiceUri"];

                    if (!string.IsNullOrWhiteSpace(blobServiceUri) &&
                        attachment.FilePath.StartsWith(blobServiceUri, StringComparison.OrdinalIgnoreCase))
                    {
                        string connectionString = _config["AzureBlobStorage:ConnectionString"]
                            ?? throw new InvalidOperationException("Azure Blob Storage connection string is missing.");

                        BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
                        Uri blobUri = new Uri(attachment.FilePath);

                        string containerName = _config["AzureBlobStorage:ContainerName"] 
                            ?? throw new InvalidOperationException("Azure Blob Storage container name is missing.");

                        string blobName = Uri.UnescapeDataString(
                            blobUri.AbsolutePath.Substring(
                                blobUri.AbsolutePath.IndexOf(containerName + "/", StringComparison.Ordinal) + containerName.Length + 1));

                        BlobClient blobClient = blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);
                        if (!await blobClient.ExistsAsync()) return NotFound("Attachment not found in Azure Blob Storage.");
                        Response.Headers["Content-Disposition"] = $"inline; filename=\"{Path.GetFileName(attachment.OriginalName)}\"";
                        Azure.Response<BlobDownloadStreamingResult> download = await blobClient.DownloadStreamingAsync();
                        string resolvedContentType = download.Value.Details.ContentType ?? contentType;
                        return File(download.Value.Content, resolvedContentType, enableRangeProcessing: true);
                    }

                    // Existing remote URLs (for example, previously stored Supabase URLs)
                    HttpClient client = _httpClientFactory.CreateClient();
                    using HttpResponseMessage remoteRes = await client.GetAsync(
                        attachment.FilePath,
                        HttpCompletionOption.ResponseHeadersRead);

                    if (!remoteRes.IsSuccessStatusCode)
                        return NotFound("Remote attachment could not be retrieved.");

                    Stream stream = await remoteRes.Content.ReadAsStreamAsync();
                    string resolvedRemoteContentType = remoteRes.Content.Headers.ContentType?.MediaType ?? contentType;

                    Response.Headers["Content-Disposition"] =
                        $"inline; filename=\"{Path.GetFileName(attachment.OriginalName)}\"";

                    return File(stream, resolvedRemoteContentType, enableRangeProcessing: true);
                }

                // Existing local disk attachments
                if (!System.IO.File.Exists(attachment.FilePath))return NotFound("File not found on disk.");

                FileStream fileStream = new FileStream(attachment.FilePath,FileMode.Open,FileAccess.Read,FileShare.Read);

                Response.Headers["Content-Disposition"] =$"inline; filename=\"{Path.GetFileName(attachment.OriginalName)}\"";

                return File(fileStream, contentType, enableRangeProcessing: true);
            }
            catch (HoneyErpException)
            {
                return NotFound("Attachment not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,"Error retrieving attachment.");
            }
        }

    }
}
