
// DTO class to hold the business type information

namespace AuthenticationProvider.Models.Data.Requests;

public class BusinessTypeDto
{
    public int Value { get; set; }
    public string DisplayName { get; set; } = null!;
}