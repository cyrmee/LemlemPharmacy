using LemlemPharmacy.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace LemlemPharmacy.DAL
{
	public interface ICustomerNotificationRepository : IDisposable
	{
		public Task<ActionResult<IEnumerable<CustomerNotificationDTO>>> GetCustomerNotification();
		public Task<ActionResult<CustomerNotificationDTO>> GetCustomerNotification(Guid id);
		public Task<ActionResult<IEnumerable<CustomerNotificationDTO>>> GetCustomerNotificationByBatchNo(string batchNo);
		public Task<ActionResult<IEnumerable<CustomerNotificationDTO>>> GetCustomerNotificationByPhoneNo(string phoneNo);
		public Task<IEnumerable<CustomerNotificationDTO>> EditCustomerNotification(Guid id, CustomerNotificationDTO customerNotification);
		public Task<IEnumerable<CustomerNotificationDTO>> AddCustomerNotification(CustomerNotificationDTO customerNotification);
	}
}
