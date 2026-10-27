using HoneyErpAPI.Common;

namespace HoneyErpAPI.Models
{
    // ── List ─────────────────────────────────────────────────────────────────
    public class GetUnitListReq : BaseApiRequest
    {
    }

    public class GetUnitListRes : BaseApiResponse
    {
        public List<UnitListItem> Items { get; set; } = new();
    }

    public class UnitListItem
    {
        public string Edid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
    }


    // ── Save (create + update) ────────────────────────────────────────────────
    public class AddUnitReq : BaseApiRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
    }

    public class AddUnitRes : BaseApiResponse
    {
        public string Edid { get; set; } = string.Empty;
    }

}