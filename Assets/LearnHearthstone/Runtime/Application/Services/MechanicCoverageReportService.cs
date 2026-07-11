using System.Linq;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;

namespace LearnHearthstone.Application.Services
{
    public static class MechanicCoverageReportService
    {
        public static MechanicCoverageReport CreateDefaultReport()
        {
            return new MechanicCoverageReport
            {
                Rows = MechanicCoverageRegistry.All.Select(entry => entry.ToReportRow()).ToList()
            };
        }
    }
}
