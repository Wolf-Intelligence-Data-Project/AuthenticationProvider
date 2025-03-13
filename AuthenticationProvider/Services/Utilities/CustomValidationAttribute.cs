using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace AuthenticationProvider.Services.Utilities;

public class RequiredBasedOnIsCompanyAttribute : ValidationAttribute
{
    private readonly string _isCompanyPropertyName;

    public RequiredBasedOnIsCompanyAttribute(string isCompanyPropertyName = "IsCompany")
    {
        _isCompanyPropertyName = isCompanyPropertyName;
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var isCompanyProperty = validationContext.ObjectType.GetProperty(_isCompanyPropertyName);

        if (isCompanyProperty == null)
        {
            throw new ArgumentException($"Property '{_isCompanyPropertyName}' was not found.");
        }

        var isCompanyValue = (bool)isCompanyProperty.GetValue(validationContext.ObjectInstance);

        if (isCompanyValue && string.IsNullOrWhiteSpace(value as string))
        {
            return new ValidationResult($"{validationContext.MemberName} is required when IsCompany is true.");
        }

        return ValidationResult.Success;
    }
}
