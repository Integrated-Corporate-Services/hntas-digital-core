using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HNTAS.Core.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class FuelTypesController : ControllerBase
    {
        private readonly ILogger<FuelTypesController> _logger;
        private readonly IFuelTypeService _fuelTypeService;
        public FuelTypesController(ILogger<FuelTypesController> logger, IFuelTypeService fuelTypeService)
        {
            _logger = logger;
            _fuelTypeService = fuelTypeService;
        }

        /// <summary>
        /// Retrieves a list of all fuel types.
        /// </summary>
        /// <returns>
        /// A list of <see cref="FuelType"/> objects wrapped in an HTTP 200 OK response.
        /// Returns HTTP 500 Internal Server Error if the retrieval fails.
        /// </returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<FuelType>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<FuelType>>> GetAllAsync()
        {
            try
            {
                var results = await _fuelTypeService.GetAllAsync();
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve fuel types.");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
