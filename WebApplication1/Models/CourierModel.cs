using HoneyErpAPI.Common;
using AttachmentEnum;
using System.Collections.Generic;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Models
{
    public class CourierMetadataItem
    {
        public int     CourierMasterId { get; set; }
        public string  Label           { get; set; } = string.Empty;
        public string  Value           { get; set; } = string.Empty;
        public bool    ShowInSlip      { get; set; } = true;
    }

    public class GetCourierListReq
    {
        public bool OnlyActive { get; set; } = false;
        public bool? IsActive { get; set; }
        public string? SearchText { get; set; }
        public PaginationRequest PaginationRequest { get; set; }
    }

    public class CourierListItem
    {
        public string   Edid          { get; set; } = string.Empty;
        public string   Name          { get; set; } = string.Empty;
        public string?  ContactPerson { get; set; }
        public string?  Mobile        { get; set; }
        public bool     IsActive      { get; set; }
    }


    public class CourierDetailsRes: BaseApiResponse
    {
        public string Edid { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public List<CourierMetadataItem>? Metadata { get; set; }
        public bool IsActive { get; set; }
    }

    public class CourierAttachmentReq
    {
        public string? AttachmentEdid { get; set; }
        public CourierAttachmentType AttachmentType { get; set; } = CourierAttachmentType.Other;
        public string? Remarks { get; set; }
    }

    public class AddCourierReq : BaseApiRequest
    {
        public string  Name          { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? Mobile        { get; set; }
        public string? Email         { get; set; }
        public string? Address       { get; set; }
        public List<CourierAttachmentReq>? AttachmentData { get; set; }
        public List<CourierMetadataItem>? Metadata { get; set; }
    }

    public class UpdateCourierReq : BaseApiRequest
    {
        public string  Edid          { get; set; } = string.Empty;
        public string  Name          { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? Mobile        { get; set; }
        public string? Email         { get; set; }
        public string? Address       { get; set; }
        public bool?   IsActive      { get; set; }
        public List<CourierAttachmentReq>? AttachmentData { get; set; }
        public List<CourierMetadataItem>? Metadata { get; set; }
    }

    public class AddCourierRes : BaseApiResponse
    {
        public string Edid { get; set; } = string.Empty;
    }

    public class UpdateCourierRes : BaseApiResponse
    {
        public bool Success { get; set; } = true;
    }
}
