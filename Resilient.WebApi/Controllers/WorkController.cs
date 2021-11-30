using Microsoft.AspNetCore.Mvc;
using Resilient.Application;
using System.Threading.Tasks;

namespace Resilient.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WorkController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Post(WorkRequest request,
            [FromServices] IWorkUseCase service)
        {
            await service.Execute(request);
            return Ok();
        }

        [HttpGet]
        public IActionResult Health() => Ok();
    }
}
