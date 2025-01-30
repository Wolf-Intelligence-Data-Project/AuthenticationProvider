// Interface for the BusinessTypeService
// Defines the contract for retrieving business types and their display names
using AuthenticationProvider.Models.Data.Dtos;
using AuthenticationProvider.Services;

namespace AuthenticationProvider.Interfaces.Services;

public interface IBusinessTypeService
{
    // Method to retrieve all business types with their corresponding display names
    List<BusinessTypeDto> GetBusinessTypes();
}
