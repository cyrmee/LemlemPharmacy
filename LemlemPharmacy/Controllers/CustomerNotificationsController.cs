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
        private readonly LemlemPharmacyContext _context;
		private readonly IMedicineRepository _medicineRepository;

		public CustomerNotificationsController(LemlemPharmacyContext context, IMedicineRepository medicineRepository)
        {
            _context = context;
            _medicineRepository = medicineRepository;
        }

        // GET: api/CustomerNotifications
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerNotificationDTO>>> GetCustomerNotification()
        {
            var result = await _context.CustomerNotification.ToListAsync();
            List<CustomerNotificationDTO> customerNotificationDTOs = new List<CustomerNotificationDTO>();
            foreach (var item in result)
                customerNotificationDTOs.Add(new CustomerNotificationDTO(item));

            return Ok(customerNotificationDTOs);
        }

        // GET: api/CustomerNotifications/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerNotificationDTO>> GetCustomerNotification(Guid id)
        {
            var customerNotification = await _context.CustomerNotification.FindAsync(id);

            if (customerNotification == null)
            {
                return NotFound();
            }

            return new CustomerNotificationDTO(customerNotification);
        }

		// GET: api/CustomerNotifications/5
		[HttpGet("batchNo/{batchNo}")]
		public async Task<ActionResult<IEnumerable<CustomerNotificationDTO>>> GetCustomerNotificationByBatchNo(string batchNo)
		{
			var result = await _context.CustomerNotification.FromSqlRaw($"SpSelectCustomerNotificationByBatchNo '{batchNo}'").ToListAsync();

			if (result == null) return NotFound();

			var customerNotificationDTOs = new List<CustomerNotificationDTO>();
			foreach (var item in result)
				customerNotificationDTOs.Add(new CustomerNotificationDTO(item));

			return customerNotificationDTOs;
		}

		// GET: api/CustomerNotifications/5
		[HttpGet("phoneNo/{phoneNo}")]
		public async Task<ActionResult<IEnumerable<CustomerNotificationDTO>>> GetCustomerNotificationByPhoneNo(string phoneNo)
		{
			var result = await _context.CustomerNotification.FromSqlRaw($"SpSelectCustomerNotificationByPhone '{phoneNo}'").ToListAsync();

			if (result == null) return NotFound();

			var customerNotificationDTOs = new List<CustomerNotificationDTO>();
			foreach (var item in result)
				customerNotificationDTOs.Add(new CustomerNotificationDTO(item));

			return customerNotificationDTOs;
		}

		// PUT: api/CustomerNotifications/5
		// To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
		[HttpPut("id/{id}")]
        public async Task<IActionResult> PutCustomerNotification(Guid id, CustomerNotificationDTO customerNotification)
        {
            if (id != customerNotification.Id)
            {
                return BadRequest();
            }

            _context.Entry(customerNotification).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerNotificationExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
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
        public async Task<IActionResult> DeleteCustomerNotification(Guid id)
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
