using HoneyErpAPI.Models;
using HoneyErpAPI.Repository;

namespace HoneyErpAPI.Services
{
    public class DashboardService
    {
        private readonly IDashboardRepository _repo;

        public DashboardService(IDashboardRepository repo)
        {
            _repo = repo;
        }

        public DashboardTodaySummaryRes GetSummary(string? period, string? dateString, int? timezoneOffsetMinutes)
        {
            return _repo.GetSummary(period, dateString, timezoneOffsetMinutes);
        }

        public SalesReportRes GetSalesReport(string? period, string? dateString, int? timezoneOffsetMinutes)
        {
            return _repo.GetSalesReport(period, dateString, timezoneOffsetMinutes);
        }
    }
}
