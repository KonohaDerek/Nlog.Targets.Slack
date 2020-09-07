using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Nlog.Targets.Slack.Demo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {

        private readonly ILogger _Logger;

        public ValuesController(ILogger<ValuesController> logger)
        {
            _Logger = logger;
        }

        [HttpGet]
        public IActionResult GetAction()
        {
            _Logger.LogError(new Exception("測試") , $"{GetType().Name} catch exception");
            return Ok("1");
        }
    }
}
