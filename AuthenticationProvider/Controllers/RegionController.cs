using AuthenticationProvider.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace AuthenticationProvider.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegionController : ControllerBase
    {
        // GET: api/region
        [HttpGet]
        public IActionResult GetRegions()
        {
            var regions = Enum.GetValues(typeof(Region))
                .Cast<Region>()
                .Select(r => new
                {
                    Value = (int)r,  // Enum value (as an integer)
                    DisplayName = GetEnumDisplayName(r)  // DisplayName from the DisplayAttribute
                })
                .ToList();

            return Ok(regions);
        }

        private string GetEnumDisplayName(Region region)
        {
            var fieldInfo = region.GetType().GetField(region.ToString());
            var attribute = fieldInfo.GetCustomAttribute<DisplayAttribute>();
            return attribute?.Name ?? region.ToString(); // fallback to enum name
        }
    }
}
