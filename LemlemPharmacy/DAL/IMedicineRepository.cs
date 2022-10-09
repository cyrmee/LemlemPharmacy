using LemlemPharmacy.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace LemlemPharmacy.DAL
{
	public interface IMedicineRepository : IDisposable
	{
		public Task<MedicineDTO> GetMedicine(Guid id);
		public Task<IEnumerable<MedicineDTO>> GetAllMedicine();
		public Task<IEnumerable<MedicineDTO>> GetMedicineByBatchNo(string batchNo);
		public Task<IEnumerable<MedicineDTO>> GetMedicineByCategory(string category);
		public Task<IEnumerable<MedicineDTO>> UpdateMedicineWithoutQuantity([FromBody] UpdateMedicineWithoutQuantityDTO medicine);
		public Task<IEnumerable<MedicineDTO>> UpdateMedicineQuantity([FromBody] UpdateMedicineQuantityDTO medicine);
		public Task<IEnumerable<MedicineDTO>> AddMedicineQuantity([FromBody] AddMedicineQuantityDTO medicine);
		public Task<IEnumerable<MedicineDTO>> RemoveMedicine([FromBody] RemoveMedicineDTO medicine);
		public Task<IEnumerable<MedicineDTO>> AddMedicine([FromBody] AddMedicineDTO medicine);

	}
}
