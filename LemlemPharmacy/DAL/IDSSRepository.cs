using LemlemPharmacy.DTOs;
using LemlemPharmacy.Models;

namespace LemlemPharmacy.DAL
{
	public interface IDssRepository : IDisposable
	{
		public Task<IEnumerable<dynamic>> GetFullRUCReport();
		public Task<IEnumerable<dynamic>> GetDamagedGraphByCategory();
		public Task<IEnumerable<dynamic>> GetSoldGraphByCategory();
		public Task<IEnumerable<dynamic>> GetInStockGraphByCategory();
		public Task<IEnumerable<dynamic>> GetProfitLossReport();
		public Task<IEnumerable<dynamic>> GetProfitLossReportByDate(DateRangeDTO dateRange);
	}
}
