using AuthenticationProvider.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationProvider.Controllers;

/// <summary>
/// Controller responsible for handling business types (used for dropdown menu in frontend).
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class BusinessTypeController : ControllerBase
{
    private readonly IBusinessTypeService _businessTypeService;
    private readonly ILogger<BusinessTypeController> _logger;

    public BusinessTypeController(IBusinessTypeService businessTypeService, ILogger<BusinessTypeController> logger)
    {
        _businessTypeService = businessTypeService ?? throw new ArgumentNullException(nameof(businessTypeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves a list of all available business types for use in frontend dropdown menus.
    /// Each business type is represented by an integer value and its corresponding display name.
    /// </summary>
    /// <returns>A list of business types, each containing an integer value and a display name.</returns>
    [HttpGet]
    public IActionResult GetBusinessTypes()
    {
        try
        {
            // Fetch business types from the service
            var businessTypes = _businessTypeService.GetBusinessTypes();

            if (businessTypes == null || businessTypes.Count == 0)
            {
                // Log warning if no business types found
                _logger.LogWarning("No business types found.");

                // Return a 404 Not Found if no business types were found
                return NotFound(new 
                {
                    ErrorCode = "RESOURCE_NOT_FOUND",
                    ErrorMessage = "Inga företagskategorier hittades.",
                    ErrorDetails = "Det finns inga tillgängliga företagskategorier i systemet."
                });
            }

            // Return business types as a JSON response
            return Ok(businessTypes);
        }
        catch (Exception ex)
        {
            // Log the exception for debugging and monitoring
            _logger.LogError(ex, "Error occurred while retrieving business types.");

            // Return 500 Internal Server Error if an error occurs
            return StatusCode(500, new
            {
                ErrorCode = "BUSINESS_TYPE_FETCH_FAILED",
                ErrorMessage = "Ett fel inträffade vid hämtning av företagskategorier.",
                ErrorDetails = "Ett oväntat fel inträffade när företagskategorier hämtades."
            });
        }
    }
}
