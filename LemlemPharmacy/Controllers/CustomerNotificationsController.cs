using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LemlemPharmacy.Data;
using LemlemPharmacy.Models;
using LemlemPharmacy.DTOs;
using Microsoft.AspNetCore.Mvc.Routing;
using LemlemPharmacy.Services;
using LemlemPharmacy.DAL;

namespace LemlemPharmacy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerNotificationsController : ControllerBase
    {
		private readonly ICustomerNotificationRepository _customerNotificationRepository;
		private readonly IMedicineRepository _medicineRepository;

		public CustomerNotificationsController(ICustomerNotificationRepository customerNotificationRepository, IMedicineRepository medicineRepository)
        {
            _customerNotificationRepository = customerNotificationRepository;
            _medicineRepository = medicineRepository;
        }

        // GET: api/CustomerNotifications
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerNotificationDTO>>> GetCustomerNotification()
        {
            try
            {
                return Ok(await _customerNotificationRepository.GetCustomerNotification());
            }
            catch(Exception e)
            {
                return BadRequest(new Response()
                {
                    Status = "Error",
                    Message = e.Message
                });
            }
        }

        // GET: api/CustomerNotifications/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerNotificationDTO>> GetCustomerNotification(Guid id)
        {
            try
            {
                return Ok(await _customerNotificationRepository.GetCustomerNotification(id));
            }
            catch(Exception e)
            {
                return BadRequest(new Response()
                {
                    Status = "Error",
                    Message = e.Message
                });
            }
        }

		// GET: api/CustomerNotifications/5
		[HttpGet("batchNo/{batchNo}")]
		public async Task<ActionResult<IEnumerable<CustomerNotificationDTO>>> GetCustomerNotificationByBatchNo(string batchNo)
		{
            try
            {
                return Ok(await _customerNotificationRepository.GetCustomerNotificationByBatchNo(batchNo));
            }
            catch(Exception e)
            {
				return BadRequest(new Response()
				{
					Status = "Error",
					Message = e.Message
				});
			}
		}

		// GET: api/CustomerNotifications/5
		[HttpGet("phoneNo/{phoneNo}")]
		public async Task<ActionResult<IEnumerable<CustomerNotificationDTO>>> GetCustomerNotificationByPhoneNo(string phoneNo)
		{
			try
			{
				return Ok(await _customerNotificationRepository.GetCustomerNotificationByPhoneNo(phoneNo));
			}
			catch (Exception e)
			{
				return BadRequest(new Response()
				{
					Status = "Error",
					Message = e.Message
				});
			}
		}

		// PUT: api/CustomerNotifications/5
		// To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
		[HttpPut("id/{id}")]
        public async Task<ActionResult> PutCustomerNotification(Guid id, CustomerNotificationDTO customerNotification)
        {
            try
            {
                return Ok(await _customerNotificationRepository.EditCustomerNotification(id, customerNotification));
            }
            catch(Exception e)
            {
				return BadRequest(new Response()
				{
					Status = "Error",
					Message = e.Message
				});
			}
        }

        [HttpGet("sendtoall")]
        public async Task<ActionResult<IEnumerable<CustomerNotificationDTO>>> SendSMSToCustomers()
        {
            var result = _context.CustomerNotification.ToList();
            if (result == null) return Ok(new Response()
            {
                Status = "Ok",
                Message = "No pending notification to be sent."
            });

            var meds = (List<MedicineDTO>)await _medicineRepository.GetAllMedicine();

			for (int i = 0; i < result.Count; i++)
                SMSService.SendSMS(
                        result[i].PhoneNo,
                        $"Please get your {meds[0].Description}.\n" + $"Sincerley,\n" + $"Lemlem Pharmacy" + $"");


            return Ok(new Response()
            {
                Status = "Success",
                Message = "SMS notifications sent."
            });
        }

		//[HttpPut("sendToCustomer")]
		//public async Task<ActionResult<IEnumerable<CustomerNotificationDTO>>> SendSMSToCustomers([FromBody] CustomerNotificationDTO customerNotificationDTO)
		//{
		//	var result = await _context.CustomerNotification.ToListAsync();
		//	if (result == null) return Ok(new Response()
		//	{
		//		Status = "Ok",
		//		Message = "No pending notification to be sent."
		//	});

		//	TwilioClient.Init(accountSid, authToken);
		//	var today = DateTime.Now;
		//	MessageResource message;
  //          MedicineDTO medicine;
		//	for (int i = 0; i < result.Count; i++)
		//	{
  //              medicine = await GetMedicineAsync(result[i].BatchNo);

		//		if (result[i].EndDate < today)
		//			result.RemoveAt(i);
		//		else if (result[i].EndDate == today)
		//			message = await MessageResource.CreateAsync(
		//			to: new PhoneNumber($"{result[i].PhoneNo}"),
		//			from: new PhoneNumber("+17087669848"),
		//			body: $"Please get your {medicine.Description} soon!");
		//	}
		//	return Ok(new Response()
		//	{
		//		Status = "Success",
		//		Message = "SMS notifications sent."
		//	});
		//}




		// POST: api/CustomerNotifications
		// To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754

		[HttpPost]
        public async Task<ActionResult<CustomerNotificationDTO>> PostCustomerNotification(CustomerNotification customerNotification)
        {
            try
            {
				_context.CustomerNotification.Add(customerNotification);
				await _context.SaveChangesAsync();
				return CreatedAtAction("GetCustomerNotification", new { id = customerNotification.Id }, customerNotification);
			}
            catch(Exception e)
            {
                return BadRequest(new Response()
                {
                    Status = "Error",
                    Message = $"{e.Message}"
                });
            }
        }

        // DELETE: api/CustomerNotifications/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCustomerNotification(Guid id)
        {
            var customerNotification = await _context.CustomerNotification.FindAsync(id);
            if (customerNotification == null)
            {
                return NotFound();
            }

            _context.CustomerNotification.Remove(customerNotification);
            await _context.SaveChangesAsync();

            return NoContent();
        }

		private bool CustomerNotificationExists(Guid id)
        {
            return _context.CustomerNotification.Any(e => e.Id == id);
        }
    }
}
