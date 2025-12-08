using InventoryServer.Models;
using InventoryServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace InventoryServer.Controllers
{
    [Route("api/inventory")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;
        private readonly IConfiguration _configuration;

        public InventoryController(IInventoryService inventoryService, IConfiguration configuration)
        {
            _inventoryService = inventoryService;
            _configuration = configuration;
        }

        [HttpGet("search")]
        public async Task<ActionResult<ResponseEnvelope<SearchResult>>> Search(
            [FromQuery] string criteria = "",
            [FromQuery] string by = "PartNumber",
            [FromQuery] string branches = "",
            [FromQuery] bool onlyAvailable = false,
            [FromQuery] int page = 0,
            [FromQuery] int size = 20,
            [FromQuery] string sort = "",
            [FromQuery] bool fail = false)
        {
            try
            {
                // Simulate network delay using configuration
                var delay = _configuration.GetValue<int>("SimulatedDelay", 0);
                if (delay > 0)
                {
                    await Task.Delay(delay);
                }

                if (fail)
                {
                    return StatusCode(500, ResponseEnvelope<SearchResult>.Failure("Simulated failure"));
                }

                var request = new InventorySearchRequest
                {
                    Criteria = criteria,
                    By = by,
                    Branches = branches,
                    OnlyAvailable = onlyAvailable,
                    Page = page,
                    Size = size,
                    Sort = sort
                };

                var result = await _inventoryService.SearchInventoryAsync(request);
                return Ok(ResponseEnvelope<SearchResult>.Success(result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ResponseEnvelope<SearchResult>.Failure(ex.Message));
            }
        }

        [HttpGet("availability/peak")]
        public async Task<ActionResult<ResponseEnvelope<AvailabilityResult>>> GetPeakAvailability(
            [FromQuery] string partNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(partNumber))
                {
                    return BadRequest(ResponseEnvelope<AvailabilityResult>.Failure("Part number is required"));
                }

                var result = await _inventoryService.GetPeakAvailabilityAsync(partNumber);
                return Ok(ResponseEnvelope<AvailabilityResult>.Success(result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ResponseEnvelope<AvailabilityResult>.Failure(ex.Message));
            }
        }

        [HttpGet("health")]
        public async Task<ActionResult<object>> Health()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }
    }
}
