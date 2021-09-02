using Microsoft.AspNetCore.Mvc;
using QueueWebApi.Application;
using System.Threading.Tasks;

namespace QueueWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WorkController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Post(WorkRequest request,
            [FromServices] IWorkService service)
        {
            await service.Execute(request);
            return Ok();
        }
    }
}
