using LemlemPharmacy.Data;
using LemlemPharmacy.Models;
using Microsoft.EntityFrameworkCore;

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

		public async Task<IEnumerable<dynamic>> GetGraphByCategory()
		{
			var result = await (from binCard in _context.Set<BinCard>()
								join medicine in _context.Set<Medicine>()
									on binCard.BatchNo equals medicine.BatchNo
								where binCard.Damaged == 1
								group new { medicine.Category, binCard.AmountRecived } by new { medicine.Category } into m
								select new
								{
									m.Key.Category,
									Amount = m.Sum(m => m.AmountRecived) * -1
								}
								).ToListAsync();

			if (result == null) throw new Exception("Record not found!");
			return result;
		}

		public async Task<IEnumerable<dynamic>> GetProfitLossReport()
		{
			var result = await (from soldMedicine in _context.Set<SoldMedicine>()
								join medicine in _context.Set<Medicine>()
									on soldMedicine.MedicineId equals medicine.Id
								join binCard in _context.Set<BinCard>()
									on soldMedicine.MedicineId equals binCard.MedicineId
								where binCard.Damaged == 1
								group new {
									soldMedicine.MedicineId,
									MedicineName = medicine.Description,
									InStock = medicine.Quantity,
									medicine.Price,
									SoldQuantity =  soldMedicine.Quantity,
									soldMedicine.SellingPrice,
									binCard.AmountRecived
								} by new { medicine.BatchNo, medicine.Description } into m
								select new
								{
									m.Key.BatchNo,
									m.Key.Description,
									SoldQuantity  = m.Sum(m => m.SoldQuantity),
									SellingPrice = m.Sum(m => m.Price * 1.25),
									MedicineCost = m.Sum(m => m.Price),
									Damaged = m.Sum(m => m.AmountRecived * -1),
									Profit = m.Sum(m => m.SellingPrice) - m.Sum(m => m.Price * m.SoldQuantity) + (m.Sum(m => m.AmountRecived) * m.Sum(m => m.Price))
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
