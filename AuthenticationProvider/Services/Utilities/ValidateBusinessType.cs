using System.ComponentModel.DataAnnotations;
using System.Reflection;
using AuthenticationProvider.Models.Enums;

namespace AuthenticationProvider.Services.Utilities;

/// <summary>
/// Custom validation attribute to validate that the business type display name matches a valid enum display name.
/// </summary>
public class ValidateBusinessTypeAttribute : ValidationAttribute
{
    /// <summary>
    /// Validates that the provided value is a valid business type display name from the BusinessTypeEnum.
    /// </summary>
    /// <param name="value">The value to be validated (should be a display name string).</param>
    /// <returns>True if the value is a valid business type display name; otherwise, false.</returns>
    public override bool IsValid(object value)
    {
        if (value is string displayName)
        {
            // Check if the display name exists in the BusinessTypeEnum's display names
            return Enum.GetValues(typeof(BusinessTypeEnum))
                .Cast<BusinessTypeEnum>()
                .Any(enumVal => GetEnumDisplayName(enumVal).Equals(displayName, StringComparison.OrdinalIgnoreCase));
        }
        return false;
    }

    /// <summary>
    /// Retrieves the display name of an enum value.
    /// </summary>
    /// <param name="businessType">The business type enum value.</param>
    /// <returns>The display name for the given enum value, or the enum name as fallback if no display name is found.</returns>
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
            return businessType.ToString();
        }
    }
}
