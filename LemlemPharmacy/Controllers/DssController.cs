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

		[HttpGet("damagedGraphByCategory")]
		public async Task<ActionResult<IEnumerable<GraphByCategoryDTO>>> GetDamagedGraphByCategory()
		{
			try
			{
				return Ok(await _dssRepository.GetDamagedGraphByCategory());
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

		[HttpGet("soldGraphByCategory")]
		public async Task<ActionResult<IEnumerable<GraphByCategoryDTO>>> GetSoldGraphByCategory()
		{
			try
			{
				return Ok(await _dssRepository.GetSoldGraphByCategory());
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

		[HttpGet("inStockGraphByCategory")]
		public async Task<ActionResult<IEnumerable<GraphByCategoryDTO>>> GetInStockGraphByCategory()
		{
			try
			{
				return Ok(await _dssRepository.GetInStockGraphByCategory());
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

		[HttpGet("profitlossbydate")]
		public async Task<ActionResult<IEnumerable<dynamic>>> GetProfitLossReportByDate([FromQuery] DateRangeDTO dateRange)
		{
			try
			{
				return Ok(await _dssRepository.GetProfitLossReportByDate(dateRange));
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
