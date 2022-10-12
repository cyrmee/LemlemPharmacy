using LemlemPharmacy.DAL;
using LemlemPharmacy.DTOs;
using Microsoft.AspNetCore.Mvc;

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

		[HttpGet("profitloss")]
		public async Task<ActionResult<IEnumerable<dynamic>>> GetProfitLossReport()
		{
			try
			{
				return Ok(await _dssRepository.GetProfitLossReport());
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
