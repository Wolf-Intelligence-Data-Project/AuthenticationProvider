using AuthenticationProvider.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace AuthenticationProvider.Controllers
{
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
                .Select(e => new
                {
                    Value = (int)e,  // Enum value as an integer
                    DisplayName = GetEnumDisplayName(e)  // DisplayName from the DisplayAttribute
                })
                .ToList();

            return Ok(businessTypes);
        }

        private string GetEnumDisplayName(BusinessType businessType)
        {
            var fieldInfo = businessType.GetType().GetField(businessType.ToString());
            var attribute = fieldInfo.GetCustomAttribute<DisplayAttribute>();
            return attribute?.Name ?? businessType.ToString(); // fallback to enum name
        }
    }
}
