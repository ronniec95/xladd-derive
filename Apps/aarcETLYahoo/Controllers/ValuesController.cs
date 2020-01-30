using System.Collections.Generic;
using AARC.RDS;
using AARC.Repository.ORM;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace aarcYahooFinETL.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        protected readonly ILogger _logger;
        protected readonly IServiceScopeFactory _serviceScopeFactory;
        protected IClosePriceDataContext _context;

        public ValuesController(AARCContext context, IServiceScopeFactory serviceScopeFactory, ILogger<ValuesController> logger)
        {
            _context = context;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }
        // GET api/values
        [HttpGet]
        public IEnumerable<dynamic> Get()
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var repository = scopedServices.GetRequiredService<ClosingPriceOrmRepository> ();

                return repository.GetWeekSummary();
            }
        }
    }
}
