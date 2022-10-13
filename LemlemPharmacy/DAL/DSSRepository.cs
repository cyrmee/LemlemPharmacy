using LemlemPharmacy.Data;
using LemlemPharmacy.DTOs;
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

		public async Task<IEnumerable<dynamic>> GetDamagedGraphByCategory()
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

		public async Task<IEnumerable<dynamic>> GetSoldGraphByCategory()
		{
			var result = await (from binCard in _context.Set<BinCard>()
								join medicine in _context.Set<Medicine>()
									on binCard.BatchNo equals medicine.BatchNo
								where binCard.Damaged == 2
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

		public async Task<IEnumerable<dynamic>> GetInStockGraphByCategory()
		{
			var result = await (from medicine in _context.Set<Medicine>()
								group new { medicine.Category, medicine.Quantity } by new { medicine.Category } into m
								select new
								{
									m.Key.Category,
									Amount = m.Sum(m => m.Quantity)
								}
								).ToListAsync();

			if (result == null) throw new Exception("Record not found!");
			return result;
		}

		public async Task<IEnumerable<dynamic>> GetProfitLossReport()
		{
			var result = await JoinedData();
			if (result == null) throw new Exception("Record not found!");
			return result;
		}

		public async Task<IEnumerable<dynamic>> GetProfitLossReportByDate(DateRangeDTO dateRange)
		{
			var result = await (from soldMedicine in _context.Set<SoldMedicine>().DefaultIfEmpty()
								join medicine in _context.Set<Medicine>().DefaultIfEmpty()
									on soldMedicine.MedicineId equals medicine.Id
								join binCard in _context.Set<BinCard>().DefaultIfEmpty()
									on soldMedicine.MedicineId equals binCard.MedicineId
								where binCard.Damaged > 0 && (dateRange.StartDate <= soldMedicine.SellingDate && soldMedicine.SellingDate <= dateRange.EndDate)
								group new
								{
									soldMedicine.MedicineId,
									medicine.Description,
									InStock = medicine.Quantity,
									medicine.Price,
									SoldQuantity = soldMedicine.Quantity,
									soldMedicine.SellingPrice,
									AmountRecived =
										(
											binCard.Damaged == 1 ? binCard.AmountRecived :
											binCard.Damaged == 2 ? 0 : 0
										),
									Status = binCard.Damaged
								} by new { medicine.BatchNo, medicine.Description } into m
								select new
								{
									m.Key.BatchNo,
									m.Key.Description,
									SoldQuantity = m.Sum(m => m.SoldQuantity),
									SellingPrice = ((from med in _context.Set<Medicine>() where med.BatchNo == m.Key.BatchNo select med.Price).ToList()[0] * 1.25),
									MedicineCost = (from med in _context.Set<Medicine>() where med.BatchNo == m.Key.BatchNo select med.Price).ToList()[0],
									Damaged = m.Sum(m => m.AmountRecived * -1),
									Profit =
									m.Sum(m => m.SellingPrice) -
									(m.Sum(m => m.SoldQuantity) * (from med in _context.Set<Medicine>() where med.BatchNo == m.Key.BatchNo select med.Price).ToList()[0]) +
									(m.Sum(m => m.AmountRecived) * (from med in _context.Set<Medicine>() where med.BatchNo == m.Key.BatchNo select med.Price).ToList()[0])
								}
								).ToListAsync();

			if (result == null) throw new Exception("Record not found!");
			return result;
		}

		internal async Task<IEnumerable<dynamic>> JoinedData()
		{
			return await (from soldMedicine in _context.Set<SoldMedicine>().DefaultIfEmpty()
							   join medicine in _context.Set<Medicine>().DefaultIfEmpty()
								   on soldMedicine.MedicineId equals medicine.Id
							   join binCard in _context.Set<BinCard>().DefaultIfEmpty()
								   on soldMedicine.MedicineId equals binCard.MedicineId
							   where binCard.Damaged > 0
							   group new
							   {
								   soldMedicine.MedicineId,
								   medicine.Description,
								   InStock = medicine.Quantity,
								   medicine.Price,
								   SoldQuantity = soldMedicine.Quantity,
								   soldMedicine.SellingPrice,
								   AmountRecived =
									   (
										   binCard.Damaged == 1 ? binCard.AmountRecived :
										   binCard.Damaged == 2 ? 0 : 0
									   ),
								   Status = binCard.Damaged
							   } by new { medicine.BatchNo, medicine.Description } into m
							   select new
							   {
								   m.Key.BatchNo,
								   m.Key.Description,
								   SoldQuantity = m.Sum(m => m.SoldQuantity),
								   SellingPrice = ((from med in _context.Set<Medicine>() where med.BatchNo == m.Key.BatchNo select med.Price).ToList()[0] * 1.25),
								   MedicineCost = (from med in _context.Set<Medicine>() where med.BatchNo == m.Key.BatchNo select med.Price).ToList()[0],
								   Damaged = m.Sum(m => m.AmountRecived * -1),
								   Profit =
								   m.Sum(m => m.SellingPrice) -
								   (m.Sum(m => m.SoldQuantity) * (from med in _context.Set<Medicine>() where med.BatchNo == m.Key.BatchNo select med.Price).ToList()[0]) +
								   (m.Sum(m => m.AmountRecived) * (from med in _context.Set<Medicine>() where med.BatchNo == m.Key.BatchNo select med.Price).ToList()[0])
							   }
								).ToListAsync();
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
