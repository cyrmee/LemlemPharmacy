using LemlemPharmacy.Models;
using System.ComponentModel.DataAnnotations;

namespace LemlemPharmacy.DTOs
{
	public class BinCardDTO
	{
		public Guid Id { get; set; }

		[Required]
		public string Invoice { get; set; } = string.Empty;

		[Required]
		public string BatchNo { get; set; } = string.Empty;

		[Required]
		[DataType(DataType.Date)]
		public DateTime DateReceived { get; set; }

		[Required]
		public int AmountRecived { get; set; }

		[Required]
		public int Damaged { get; set; }

		public BinCardDTO()
		{

		}

		public BinCardDTO(Guid id, string invoice, string batchNo, DateTime dateReceived, int amountRecived, int damaged)
		{
			Id = id;
			Invoice = invoice;
			BatchNo = batchNo;
			DateReceived = dateReceived;
			AmountRecived = amountRecived;
			Damaged = damaged;
		}

		public BinCardDTO(BinCard binCard)
		{
			Id = binCard.Id;
			Invoice = binCard.Invoice;
			BatchNo = binCard.BatchNo;
			DateReceived = binCard.DateReceived;
			AmountRecived = binCard.AmountRecived;
			Damaged = binCard.Damaged;
		}
	}
}
