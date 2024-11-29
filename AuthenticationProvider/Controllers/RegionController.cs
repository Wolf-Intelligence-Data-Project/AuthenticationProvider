using AuthenticationProvider.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace AuthenticationProvider.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RegionController : ControllerBase
{
    [HttpGet]
    public IActionResult GetRegions()
    {
        try
        {
            var regions = Enum.GetValues(typeof(Region))
                              .Cast<Region>()
                              .Select(r => new
                              {
                                  Value = (int)r,
                                  DisplayName = r.GetType()
                                                 .GetField(r.ToString())
                                                 .GetCustomAttributes(typeof(DisplayAttribute), false)
                                                 .Cast<DisplayAttribute>()
                                                 .FirstOrDefault()?.Name ?? r.ToString()
                              })
                              .ToList();

            return Ok(regions);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ett oväntat fel inträffade: {ex.Message}");
        }
    }
}
