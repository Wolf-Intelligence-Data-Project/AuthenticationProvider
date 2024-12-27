using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Models.Tokens; // Assuming you have the Company entity
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuthenticationProvider.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountVerificationController : ControllerBase
{
    private readonly IAccountVerificationTokenService _accountVerificationTokenService;
    private readonly ICompanyRepository _companyRepository;  // Assuming you have this repo for accessing Company
    private readonly ILogger<AccountVerificationController> _logger;

    public AccountVerificationController(
        IAccountVerificationTokenService accountVerificationTokenService,
        ICompanyRepository companyRepository,  // Inject the repository
        ILogger<AccountVerificationController> logger)
    {
        _accountVerificationTokenService = accountVerificationTokenService;
        _companyRepository = companyRepository;
        _logger = logger;
    }

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("No token provided for email verification.");
            return BadRequest("Token krävs.");
        }

        try
        {
            // Validate the token first
            var accountVerificationToken = await _accountVerificationTokenService.GetValidAccountVerificationTokenAsync(token);
            if (accountVerificationToken == null)
            {
                _logger.LogWarning("Invalid or expired token provided for email verification.");
                return BadRequest("Ogiltigt eller utgånget token.");
            }

            // Fetch the company related to this token
            var company = await _companyRepository.GetByIdAsync(accountVerificationToken.CompanyId);
            if (company == null)
            {
                _logger.LogWarning("No company found associated with the token.");
                return BadRequest("Företaget kunde inte hittas.");
            }

            // Mark token as used
            await _accountVerificationTokenService.MarkAccountVerificationTokenAsUsedAsync(token);

            // Optionally, revoke and delete all tokens for this company
            // This ensures that all verification tokens for this company are invalidated and removed
            await _accountVerificationTokenService.DeleteAccountVerificationTokensForCompanyAsync(company.Id);

            // Update IsVerified flag in the company entity
            company.IsVerified = true;

            // Save the company entity with the updated IsVerified flag
            await _companyRepository.UpdateAsync(company);  // Assuming UpdateAsync saves changes to the company

            // Return success response
            _logger.LogInformation("Email verified successfully for company: {CompanyId}", company.Id);
            return Redirect("http://localhost:3000/verification-success");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while verifying email for token: {Token}", token);
            return StatusCode(500, "Ett fel inträffade vid verifiering av e-postadress.");
        }
    }
}

  