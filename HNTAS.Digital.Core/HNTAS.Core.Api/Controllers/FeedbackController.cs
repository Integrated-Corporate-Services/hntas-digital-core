using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HNTAS.Core.Api.Controllers
{
    [ApiController]
    [Route("api/feedback")]

    public class FeedbackController : ControllerBase
    {

        private readonly IFeedbackService _service;

        public FeedbackController(IFeedbackService service)
        {
            _service = service;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Create(CreateFeedbackRequest request)
        {
            await _service.CreateAsync(request);
            return Ok();
        }
    }
}
