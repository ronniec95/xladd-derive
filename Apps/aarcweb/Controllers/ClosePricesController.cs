using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AARC.Model;
using AARC.Repository.EF;
using aarcweb.Interfaces.Injection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace aarcweb.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ClosePricesController : ControllerBase
    {
        protected ClosePriceDataRepository _closePricesRepository;

        public ClosePricesController(ClosePriceDataRepository provider)
        {
            _closePricesRepository = provider;
        }
        // GET: api/ClosePrices
        [HttpGet]
        public IEnumerable<UnderlyingPrice> Get() => _closePricesRepository.Get();

        // GET: api/ClosePrices/5
        [HttpGet("{id}")]
        public IEnumerable<UnderlyingPrice> Get(string id) => _closePricesRepository.Get(id);

        // POST: api/ClosePrices
        [HttpPost]
        public void Post([FromBody] UnderlyingPrice value) => _closePricesRepository.Upsert(value);

        // PUT: api/ClosePrices/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
