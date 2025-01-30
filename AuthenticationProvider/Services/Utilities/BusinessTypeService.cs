using AuthenticationProvider.Interfaces.Services;
using AuthenticationProvider.Models.Data.Dtos;
using AuthenticationProvider.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace AuthenticationProvider.Services;

// Implementation of the BusinessTypeService interface
public class BusinessTypeService : IBusinessTypeService
{
    public List<BusinessTypeDto> GetBusinessTypes()
    {
        try
        {
            return Enum.GetValues(typeof(BusinessTypeEnum))
                .Cast<BusinessTypeEnum>()
                .Select(e => new BusinessTypeDto
                {
                    Value = (int)e,
                    DisplayName = GetEnumDisplayName(e)
                })
                .ToList();
        }
        catch (Exception ex)
        {
            // Log the error if needed
            return new List<BusinessTypeDto>();
        }
    }

    private string GetEnumDisplayName(BusinessTypeEnum businessType)
    {
        try
        {
            var fieldInfo = businessType.GetType().GetField(businessType.ToString());
            var attribute = fieldInfo?.GetCustomAttribute<DisplayAttribute>();
            return attribute?.Name ?? businessType.ToString();
        }
        catch
        {
            return businessType.ToString(); // Return enum name as fallback
        }
    }
}
