using AuthenticationProvider.Models;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationProvider.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BusinessTypeController : ControllerBase
{
    [HttpGet]
    public IActionResult GetBusinessTypes()
    {
        try
        {
            var businessTypes = Enum.GetValues(typeof(BusinessType))
                                    .Cast<BusinessType>()
                                    .Select(e => new { Value = e.ToString(), DisplayName = e.ToString() })
                                    .ToList();

            return Ok(businessTypes);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ett oväntat fel inträffade: {ex.Message}");
        }
    }
}