using HoneyErpAPI.Common;

namespace HoneyErpAPI.Models
{
    public class EnumListResponse
    {
        public List<string> Items { get; set; } = new List<string>();
    }

    public class AttachmentModel
    {
        public string OriginalName { get; set; } = string.Empty;
        public string StoredName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
    }

    public class DocumentModel: BaseApiResponse
    {
        public string? DocumentEdid { get; set; }
        public string? DocumentOriginalName { get; set; }
    }

    public class FileUploadReq
    {
        public Microsoft.AspNetCore.Http.IFormFile file { get; set; }
    }
}
