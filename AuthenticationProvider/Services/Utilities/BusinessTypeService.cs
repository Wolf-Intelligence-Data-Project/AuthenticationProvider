using System.ComponentModel.DataAnnotations;
using System.Reflection;
using AuthenticationProvider.Interfaces.Utilities;
using AuthenticationProvider.Models.Data.Requests;
using AuthenticationProvider.Models.Enums;

namespace AuthenticationProvider.Services.Utilities;

/// <summary>
/// Provides functionality to retrieve business types and their display names based on the BusinessTypeEnum
/// </summary>
public class BusinessTypeService : IBusinessTypeService
{
    /// <summary>
    /// Retrieves a list of all business types with their corresponding display names.
    /// </summary>
    /// <returns>A list of <see cref="BusinessTypeDto"/> objects, each containing a business type's value and display name.</returns>
    public List<BusinessTypeDto> GetBusinessTypes()
    {
        try
        {
            // Retrieves all enum values of BusinessTypeEnum and maps them to a list of BusinessTypeDto
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
            return new List<BusinessTypeDto>(); // Return empty list in case of failure
        }
    }

    /// <summary>
    /// Retrieves the display name for a given BusinessTypeEnum value.
    /// If no display name is found, the enum's name is returned as a fallback.
    /// </summary>
    /// <param name="businessType">The business type enum value.</param>
    /// <returns>The display name for the given enum value, or the enum name as fallback.</returns>
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
