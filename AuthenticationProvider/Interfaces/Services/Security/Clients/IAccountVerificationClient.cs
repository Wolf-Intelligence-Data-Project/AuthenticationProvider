﻿using AuthenticationProvider.Models.Dtos;

namespace AuthenticationProvider.Interfaces.Services.Security.Clients;

/// <summary>
/// Defines a contract for an external client responsible for sending 
/// email verification emails. Implementing classes should handle 
/// communication with an external provider that delivers the verification email.
/// </summary>
public interface IEmailVerificationClient
{
    /// <summary>
    /// Sends an email verification request containing a verification token 
    /// to an external provider, which will then deliver the email to the user.
    /// </summary>
    /// <param name="token">A unique token used to verify the user's email.</param>
    /// <returns>True if the request was successfully processed by the provider; otherwise, false.</returns>
    Task<bool> SendVerificationEmailAsync(EmailVerificationDto sendVerificationRequest);
}
