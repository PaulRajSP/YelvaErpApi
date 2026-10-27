using HoneyErpAPI.Common;
using System.Collections.Generic;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Models
{
    public class CourierMetadataItem
    {
        public int     Id              { get; set; }
        public string? Edid            { get; set; }
        public int     CourierMasterId { get; set; }
        public string  Label           { get; set; } = string.Empty;
        public string  Value           { get; set; } = string.Empty;
        public bool    ShowInSlip      { get; set; } = true;
    }

    public class GetCourierListReq
    {
        public bool OnlyActive { get; set; } = false;
        public string? SearchText { get; set; }
        public PaginationRequest PaginationRequest { get; set; }
    }

    public class CourierListItem: BaseApiResponse
    {
        public string   Edid          { get; set; } = string.Empty;
        public int      Id            { get; set; }
        public string   Name          { get; set; } = string.Empty;
        public string?  ContactPerson { get; set; }
        public string?  Mobile        { get; set; }
        public string?  Email         { get; set; }
        public string?  Address       { get; set; }
        public bool     IsActive      { get; set; }
        public string?  DocEdid       { get; set; }
        public List<CourierMetadataItem> Metadata { get; set; } = new();
    }

    public class GetCourierListRes : BaseApiResponse
    {
        public List<CourierListItem> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize   { get; set; } = 10;
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : (TotalCount > 0 ? 1 : 0);
    }

    public class AddCourierReq : BaseApiRequest
    {
        public string  Name          { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? Mobile        { get; set; }
        public string? Email         { get; set; }
        public string? Address       { get; set; }
        public string? DocId         { get; set; }
        public string? DocEdid       { get; set; }
        public List<CourierMetadataItem>? Metadata { get; set; }
    }

    public class UpdateCourierReq : BaseApiRequest
    {
        public string  Edid          { get; set; } = string.Empty;
        public int?    Id            { get; set; }
        public string  Name          { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? Mobile        { get; set; }
        public string? Email         { get; set; }
        public string? Address       { get; set; }
        public bool?   IsActive      { get; set; }
        public string? DocId         { get; set; }
        public string? DocEdid       { get; set; }
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

