using System.ComponentModel.DataAnnotations;
using System.Security.Permissions;

namespace LemlemPharmacy.DTOs
{
	public class ProfitLossDTO
	{
		[Required]
		public Guid MedicineId { get; set; }

		[Required]
		public string BatchNo { get; set; } = string.Empty;

		[Required]
		public int InStock { get; set; }

		[Required]
		public string Description { get; set; } = string.Empty;

		[Required]
		public int SoldQuantity { get; set; }

		[Required]
		public double SellingPrice { get; set; }

		[Required]
		public double MedicineCost { get; set; }

		[Required]
		public string Invoice { get; set; } = string.Empty;

		[Required]
		public int Status { get; set; }

		[Required]
		public int AmountRecived { get; set; }

		[Required]
		public float Profit { get; set; }


		public ProfitLossDTO()
		{

		}

		public ProfitLossDTO(Guid medicineId, string batchNo, int inStock, string description, int soldQuantity, double sellingPrice, double medicineCost, string invoice, int status, int amountRecived)
		{
			MedicineId = medicineId;
			BatchNo = batchNo;
			InStock = inStock;
			Description = description;
			SoldQuantity = soldQuantity;
			SellingPrice = sellingPrice;
			MedicineCost = medicineCost;
			Invoice = invoice;
			Status = status;
			AmountRecived = amountRecived;
		}
	}
}
