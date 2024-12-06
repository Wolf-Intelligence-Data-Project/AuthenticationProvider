using AuthenticationProvider.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace AuthenticationProvider.Attributes;

public class ValidRegionAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value is Region region && Enum.IsDefined(typeof(Region), region))
        {
            return ValidationResult.Success!;
        }
        return new ValidationResult("Ogiltig region."); // Swedish for "Invalid region."
    }
}
