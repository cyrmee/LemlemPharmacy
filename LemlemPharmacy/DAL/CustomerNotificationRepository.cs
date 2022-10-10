using LemlemPharmacy.Data;
using LemlemPharmacy.DTOs;
using LemlemPharmacy.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.IIS.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using System.Text.RegularExpressions;

namespace LemlemPharmacy.DAL
{
	public class CustomerNotificationRepository : ICustomerNotificationRepository, IDisposable
	{
		private readonly LemlemPharmacyContext _context;
		private readonly string pattern = @"(\+\s*2\s*5\s*1\s*9\s*(([0-9]\s*){8}\s*))|(0\s*9\s*(([0-9]\s*){8}))";

		public CustomerNotificationRepository(LemlemPharmacyContext context)
		{
			_context = context;
		}

		public async Task<ActionResult<IEnumerable<CustomerNotificationDTO>>> GetCustomerNotification()
		{
			var result = await _context.CustomerNotification.ToListAsync();
			var customerNotificationDTOs = new List<CustomerNotificationDTO>();
			foreach (var item in result)
				customerNotificationDTOs.Add(new CustomerNotificationDTO(item));

			return customerNotificationDTOs;
		}

		public async Task<ActionResult<CustomerNotificationDTO>> GetCustomerNotification(Guid id)
		{
			var customerNotification = await _context.CustomerNotification.FindAsync(id);
			if (customerNotification == null) throw new Exception("Record not found!");
			return new CustomerNotificationDTO(customerNotification);
		}

		public async Task<ActionResult<IEnumerable<CustomerNotificationDTO>>> GetCustomerNotificationByBatchNo(string batchNo)
		{
			var result = await _context.CustomerNotification.FromSqlRaw($"SpSelectCustomerNotificationByBatchNo '{batchNo}'").ToListAsync();
			if (result == null) throw new Exception("Record not found!");
			var customerNotificationDTOs = new List<CustomerNotificationDTO>();
			foreach (var item in result)
				customerNotificationDTOs.Add(new CustomerNotificationDTO(item));
			return customerNotificationDTOs;
		}

		public async Task<ActionResult<IEnumerable<CustomerNotificationDTO>>> GetCustomerNotificationByPhoneNo(string phoneNo)
		{
			var result = await _context.CustomerNotification.FromSqlRaw($"SpSelectCustomerNotificationByPhone '{phoneNo}'").ToListAsync();
			if (result == null) throw new Exception("Record not found!");
			var customerNotificationDTOs = new List<CustomerNotificationDTO>();
			foreach (var item in result)
				customerNotificationDTOs.Add(new CustomerNotificationDTO(item));
			return customerNotificationDTOs;
		}
		public async Task<IEnumerable<CustomerNotificationDTO>> EditCustomerNotification(Guid id, CustomerNotificationDTO customerNotification)
		{
			string storedProc = $"EXEC SpUpdateCustomerNotification @Id = '{customerNotification.Id}',@PhoneNo  = '{customerNotification.PhoneNo}',@BatchNo  = '{customerNotification.BatchNo}',@Interval  = {customerNotification.Interval},@EndDate  = '{customerNotification.EndDate}',@NextDate  = '{customerNotification.NextDate}'";

			if (Regex.IsMatch(customerNotification.PhoneNo, pattern))
			{
				var result = await _context.CustomerNotification.FromSqlRaw(storedProc).ToListAsync();
				var customerNotifications = new List<CustomerNotificationDTO>();
				foreach (var item in result)
					customerNotifications.Add(new CustomerNotificationDTO(item));

				return customerNotifications;
			}
			else
				throw new Exception("Phone number not in the right format. Example: +251 91 234 5678 +251912345678");
		}

		public async Task<IEnumerable<CustomerNotificationDTO>> AddCustomerNotification(CustomerNotificationDTO customerNotification)
		{
			string storedProc = $"EXEC SpAddCustomerNotification @Id = '{customerNotification.Id}',@PhoneNo  = '{customerNotification.PhoneNo}',@BatchNo  = '{customerNotification.BatchNo}',@Interval  = {customerNotification.Interval},@EndDate  = '{customerNotification.EndDate}',@NextDate  = '{customerNotification.NextDate}'";

			if (Regex.IsMatch(customerNotification.PhoneNo, pattern))
			{
				var result = await _context.CustomerNotification.FromSqlRaw(storedProc).ToListAsync();
				var customerNotifications = new List<CustomerNotificationDTO>();
				foreach (var item in result)
					customerNotifications.Add(new CustomerNotificationDTO(item));

				return customerNotifications;
			}
			else
				throw new Exception("Phone number not in the right format. Example: +251 91 234 5678 +251912345678");
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
