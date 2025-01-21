using AuthenticationProvider.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;


// This controller is for delivering dto enum to the frontend so that it could be used for validation and dropdown menu choices
namespace AuthenticationProvider.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BusinessTypeController : ControllerBase
{
    // GET: api/businessType
    [HttpGet]
    public IActionResult GetBusinessTypes()
    {
        var businessTypes = Enum.GetValues(typeof(BusinessTypeEnum))
            .Cast<BusinessTypeEnum>()
            .Select(e => new
            {
                Value = (int)e,  // Enum value as an integer
                DisplayName = GetEnumDisplayName(e)  // DisplayName from the DisplayAttribute
            })
            .ToList();

        return Ok(businessTypes);
    }

    private string GetEnumDisplayName(BusinessTypeEnum businessType)
    {
        var fieldInfo = businessType.GetType().GetField(businessType.ToString());
        var attribute = fieldInfo.GetCustomAttribute<DisplayAttribute>();
        return attribute?.Name ?? businessType.ToString(); // fallback to enum name
    }
}
