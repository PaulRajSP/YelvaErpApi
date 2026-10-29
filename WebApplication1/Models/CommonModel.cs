using AttachmentEnum;
using HoneyErpAPI.Common;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Text.Json.Serialization;

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

    public class FileUploadReq
    {
        public Microsoft.AspNetCore.Http.IFormFile file { get; set; }
    }

    public class GetModuleAttachmentReq
    {
        public string ModuleName { get; set; }
        public string RecordEdid { get; set; }
    }

    public class AttachmentModuleReq
    {
        public AttachmentModule? ModuleName { get; set; }
        public string? RecordEdid { get; set; }
        public string? AttachmentType { get; set; }
        public string? AttachmentEdid { get; set; }
        public string? Remarks { get; set; }
    }

    public class DocumentAttachmentReq
    {
        public string? AttachmentEdid { get; set; }
        public string? AttachmentType { get; set; }
        public string? Remarks { get; set; }
    }

    public class AttachmentModuleRes : BaseApiResponse
    {
        public string? AttachmentEdid { get; set; }
        public string? AttachmentOriginalName { get; set; }
        public string? AttachmentType { get; set; }
        public string? Remarks { get; set; }
    }


}
