﻿using LemlemPharmacy.DTOs;
using LemlemPharmacy.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LemlemPharmacy.DAL
{
	public interface ISoldMedicineRepository : IDisposable
	{
		public Task<IEnumerable<SoldMedicineDTO>> SellMedicine([FromBody] SellMedicineDTO soldMedicine);
		public Task<IEnumerable<SoldMedicineDTO>> GetAllSoldMedicines();
		public Task<SoldMedicineDTO> GetSoldMedicine(Guid id);
	}
}
