using Microsoft.AspNetCore.Http;
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
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Post(WorkRequest request,
            [FromServices] IWorkUseCase useCase)
        {
            await useCase.Execute(request);
            return Accepted();
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult Health() => Ok();
    }
}
