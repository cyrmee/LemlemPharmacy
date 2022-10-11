using LemlemPharmacy.DAL;
using LemlemPharmacy.DTOs;
using LemlemPharmacy.Models;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace LemlemPharmacy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DssController : ControllerBase
    {
        private readonly IDssRepository _dssRepository;

        public DssController(IDssRepository dssRepository)
        {
            _dssRepository = dssRepository;
        }


        // GET: api/<DssController>
        [HttpGet("FullRUCRecords")]
        public async Task<ActionResult<IEnumerable<FullRucDTO>>> GetFullRucReport()
        {
            try
            {
                return Ok(await _dssRepository.GetFullRUCReport());
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

		[HttpGet("graphByCategory")]
		public async Task<ActionResult<IEnumerable<GraphByCategoryDTO>>> GetGraphbyCategory()
		{
			try
			{
				return Ok(await _dssRepository.GetGraphByCategory());
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
	}
}
