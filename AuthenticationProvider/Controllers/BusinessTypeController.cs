using AuthenticationProvider.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace AuthenticationProvider.Controllers;

/// <summary>
/// Controller for retrieving business types as an enumerable list.
/// This endpoint provides a list of available business types, including 
/// their string values and display names. It is used in frontend for dropdown (signup / non-signed user)
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class BusinessTypeController : ControllerBase
{
    /// <summary>
    /// Retrieves a list of business types.
    /// </summary>
    /// <returns>A list of business types with their values and display names.</returns>
    [HttpGet]
    public IActionResult GetBusinessTypes()
    {
        var businessTypes = Enum.GetValues(typeof(BusinessTypeEnum))
            .Cast<BusinessTypeEnum>()
            .Select(e => new
            {
                Value = e.ToString(),
                DisplayName = GetEnumDisplayName(e)
            })
            .ToList();

        return Ok(businessTypes);
    }

    /// <summary>
    /// Retrieves the display name of a given business type.
    /// </summary>
    /// <param name="businessTypeEnum">The business type enum value.</param>
    /// <returns>The display name if available; otherwise, the enum name.</returns>
    private string GetEnumDisplayName(BusinessTypeEnum businessTypeEnum)
    {
        var fieldInfo = businessTypeEnum.GetType().GetField(businessTypeEnum.ToString());
        var attribute = fieldInfo.GetCustomAttribute<DisplayAttribute>();
        return attribute?.Name ?? businessTypeEnum.ToString();
    }
}
