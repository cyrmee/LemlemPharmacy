using LemlemPharmacy.DTOs;
using LemlemPharmacy.Models;

namespace LemlemPharmacy.DAL
{
	public interface IDssRepository : IDisposable
	{
		public Task<IEnumerable<dynamic>> GetFullRUCReport();
		public Task<IEnumerable<dynamic>> GetGraphByCategory();
	}
}
