﻿namespace AuthenticationProvider.Models;

public class CaptchaVerificationResponse
{
    public bool Success { get; set; }
    public DateTime Challenge_ts { get; set; }
    public string Hostname { get; set; }
    public List<string> ErrorCodes { get; set; }
}
