using LemlemPharmacy.Data;
using LemlemPharmacy.DTOs;
using LemlemPharmacy.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace LemlemPharmacy.DAL
{
	public class DssRepository : IDssRepository, IDisposable
	{
		private readonly LemlemPharmacyContext _context;

		public DssRepository(LemlemPharmacyContext context)
		{
			_context = context;
		}

		public async Task<IEnumerable<dynamic>> GetFullRUCReport()
		{
			var result = await (from binCard in _context.Set<BinCard>()
								join medicine in _context.Set<Medicine>()
									on binCard.BatchNo equals medicine.BatchNo
								where binCard.Damaged == 1
								let amount = binCard.AmountRecived * -1
								select new
								{
									binCard.Invoice,
									binCard.BatchNo,
									binCard.DateReceived,
									amount,
									medicine.Description,
									medicine.ExpireDate,
									medicine.Category,
									medicine.Type
								}
						  ).ToListAsync();

			if (result == null) throw new Exception("Record not found!");
			return result;
		}

		private bool disposed = false;
		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					_context.Dispose();
				}
			}
			disposed = true;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}
