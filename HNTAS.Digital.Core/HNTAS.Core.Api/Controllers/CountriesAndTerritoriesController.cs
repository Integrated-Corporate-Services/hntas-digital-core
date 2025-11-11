using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HNTAS.Core.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CountriesAndTerritoriesController : ControllerBase
    {
        private readonly ILogger<CountriesAndTerritoriesController> _logger;
        private readonly ICountryAndTerritoryService _countryAndTerritoryService;
        public CountriesAndTerritoriesController(ILogger<CountriesAndTerritoriesController> logger, ICountryAndTerritoryService countryAndTerritoryService)
        {
            _logger = logger;
            _countryAndTerritoryService = countryAndTerritoryService;
        }

        /// <summary>
        /// Retrieves a list of all countries and territories.
        /// </summary>
        /// <returns>
        /// A list of <see cref="CountryAndTerritory"/> objects wrapped in an HTTP 200 OK response.
        /// Returns HTTP 500 Internal Server Error if the retrieval fails.
        /// </returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<CountryAndTerritory>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<CountryAndTerritory>>> GetAllAsync()
        {
            try
            {
                var results = await _countryAndTerritoryService.GetAllAsync();
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve CountriesAndTerritories.");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
