using AuthenticationProvider.Models.Data.Requests;

namespace AuthenticationProvider.Interfaces.Utilities;

public interface IBusinessTypeService
{
    /// <summary>
    /// Retrieves a list of all business types, each with its corresponding display name.
    /// </summary>
    /// <returns>
    /// A list of <see cref="BusinessTypeDto"/> objects, 
    /// where each object contains the value and display name for a business type.
    /// </returns>
    List<BusinessTypeDto> GetBusinessTypes();
}
