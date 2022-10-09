using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LemlemPharmacy.Data;
using LemlemPharmacy.Models;
using LemlemPharmacy.DTOs;
using LemlemPharmacy.DAL;

namespace LemlemPharmacy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BinCardsController : ControllerBase
    {
        private readonly IBinCardRepository _binCardRepository;

        public BinCardsController(IBinCardRepository binCardRepository)
        {
            _binCardRepository = binCardRepository;
        }

        // GET: api/BinCards
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BinCardDTO>>> GetAllBinCards()
        {
            try
            {
                return Ok(await _binCardRepository.GetAllBinCards());
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

		}

        // GET: api/BinCards/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BinCardDTO>> GetBinCard(Guid id)
        {
            try
            {
                return Ok(await _binCardRepository.GetBinCard(id));
			}
            catch(Exception e)
            {
				return BadRequest(e.Message);
			}
        }

		[HttpGet("batchNo/{batchNo}")]
		public async Task<ActionResult<IEnumerable<BinCardDTO>>> GetBinCardByBatchNo(string batchNo)
		{
            try
            {
                return Ok(await _binCardRepository.GetBinCardByBatchNo(batchNo));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
		}

		[HttpGet("byDate/")]
		public async Task<ActionResult<IEnumerable<BinCardDTO>>> GetBinCardByDate([FromQuery] BinCardDateRangeDTO binCardDateRangeDTO)
		{
			try
			{
				return Ok(await _binCardRepository.GetBinCardByDate(binCardDateRangeDTO));
			}
			catch (Exception e)
			{
				return BadRequest(e.Message);
			}
		}
	}
}
