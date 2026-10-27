using HoneyErpAPI.Common;

namespace HoneyErpAPI.Models
{
    public class DocumentHtmlRes : BaseApiResponse
    {
        public string DocumentType { get; set; } = string.Empty;
        public string Identifier { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
    }

    public class DocumentTypeListRes : BaseApiResponse
    {
        public List<string> Items { get; set; } = new List<string>();
    }

    public class BulkDocumentRes : BaseApiResponse
    {
        public string Status { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public List<string> OrderNumbers { get; set; } = new List<string>();
        public string HtmlContent { get; set; } = string.Empty;
    }

    public class BlobUploadRes : BaseApiResponse
    {
        public bool Uploaded { get; set; }
        public string BlobName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string BlobUrl { get; set; } = string.Empty;
        public string ContainerName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string ContentType { get; set; } = string.Empty;
    }
}

