using Microsoft.AspNetCore.Mvc;
using ResilientWebApi.Application;
using System.Threading.Tasks;

namespace ResilientWebApi.Controllers
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
