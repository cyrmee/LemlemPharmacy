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
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FullRucDTO>>> Get()
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

        // GET api/<DssController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<DssController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<DssController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<DssController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
