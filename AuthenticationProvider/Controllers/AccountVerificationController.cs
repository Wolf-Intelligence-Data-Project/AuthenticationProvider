using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Interfaces.Services;
using AuthenticationProvider.Models;
using Microsoft.AspNetCore.Mvc;


namespace AuthenticationProvider.Controllers;
[ApiController]
[Route("api/[controller]")]
public class AccountVerificationController : ControllerBase
{
    private readonly IAccountVerificationTokenService _accountVerificationTokenService;
    private readonly IAccountVerificationService _accountVerificationService; // Add the service here
    private readonly ICompanyRepository _companyRepository;
    private readonly ILogger<AccountVerificationController> _logger;

    public AccountVerificationController(
        IAccountVerificationTokenService accountVerificationTokenService,
        IAccountVerificationService accountVerificationService, // Inject the service here
        ICompanyRepository companyRepository,
        ILogger<AccountVerificationController> logger)
    {
        _accountVerificationTokenService = accountVerificationTokenService;
        _accountVerificationService = accountVerificationService; // Initialize the injected service here
        _companyRepository = companyRepository;
        _logger = logger;
    }

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("No token provided for email verification.");
            return BadRequest("Något gick fel.");
        }

        try
        {
            // Validate the token first
            var accountVerificationToken = await _accountVerificationTokenService.GetValidAccountVerificationTokenAsync(token);
            if (accountVerificationToken == null)
            {
                _logger.LogWarning("Invalid or expired token provided for email verification.");
                return BadRequest("Ogiltigt session.");
            }

            // Fetch the company related to this token
            var company = await _companyRepository.GetByIdAsync(accountVerificationToken.CompanyId);
            if (company == null)
            {
                _logger.LogWarning("No company found associated with the token.");
                return BadRequest("Kontot kunde inte hittas.");
            }

           

            // Mark token as used
            await _accountVerificationTokenService.MarkAccountVerificationTokenAsUsedAsync(token);

            // Optionally, revoke and delete all tokens for this company
            await _accountVerificationTokenService.DeleteAccountVerificationTokensForCompanyAsync(company.Id);

            // Update IsVerified flag in the company entity
            company.IsVerified = true;

            // Save the company entity with the updated IsVerified flag
            await _companyRepository.UpdateAsync(company);

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


    [HttpPost("resend-verification-email")]
    public async Task<IActionResult> ResendVerificationEmail([FromBody] EmailDto emailDto)
    {
        if (string.IsNullOrEmpty(emailDto.Email))
        {
            _logger.LogWarning("No email provided for resending verification email.");
            return BadRequest(new { message = "Email is required." });
        }

        try
        {
            // Fetch the company by email
            var company = await _companyRepository.GetByEmailAsync(emailDto.Email);
            if (company == null)
            {
                _logger.LogWarning("Company not found with email: {Email}", emailDto.Email);
                return BadRequest(new { message = "Company not found." });
            }

            // Generate a new account verification token for the company
            var token = await _accountVerificationTokenService.CreateAccountVerificationTokenAsync(company.Id);

            // Send the token to the email verification provider (using the injected service)
            var emailSent = await _accountVerificationService.SendVerificationEmailAsync(token);

            if (!emailSent)
            {
                return StatusCode(500, new { message = "Failed to send verification email." });
            }

            _logger.LogInformation("Verification email resent to company with email: {Email}", emailDto.Email);

            // Return success response
            return Ok(new { message = "Verification email resent." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while resending the verification email for email: {Email}", emailDto.Email);
            return StatusCode(500, new { message = "An error occurred while resending the verification email." });
        }
    }




}
