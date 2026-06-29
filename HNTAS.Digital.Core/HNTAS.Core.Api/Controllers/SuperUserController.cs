using HNTAS.Core.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HNTAS.Core.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SuperUserController : Controller
    {
        private readonly ISuperUserService _superUserService;

        public SuperUserController(ISuperUserService superUserService)
        {
            _superUserService = superUserService;
        }

        /// <summary>
        /// Check if a user is a Super User by their email ID
        /// </summary>
        /// <remarks>
        /// Validates whether the given email ID exists in the super users record collection.
        /// </remarks>
        /// <param name="emailId">The email ID of the user to check.</param>
        /// <returns>
        /// A <see cref="StatusCodes.Status200OK"/> response with a boolean indicating if the user is a super user.
        /// </returns>
        [HttpGet("is-super-user/{emailId}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<ActionResult<bool>> IsSuperUser(string emailId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(emailId))
                {
                    return Ok(false);
                }

                bool isSuperUser = await _superUserService.IsSuperUserAsync(emailId);
                return Ok(isSuperUser);
            }
            catch (Exception)
            {
                // Fallback to false for any negative execution paths or exceptions
                return Ok(false);
            }
        }
    }
}
