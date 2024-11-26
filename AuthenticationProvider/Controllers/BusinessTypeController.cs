using AuthenticationProvider.Models;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationProvider.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BusinessTypeController : ControllerBase
{
    // GET: api/businessType
    [HttpGet]
    public IActionResult GetBusinessTypes()
    {
        var businessTypes = Enum.GetValues(typeof(BusinessType))
                                .Cast<BusinessType>()
                                .Select(e => new { Value = e.ToString(), DisplayName = e.ToString() })
                                .ToList();

        return Ok(businessTypes);
    }
}
