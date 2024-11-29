using System.ComponentModel.DataAnnotations;

public class JwtSettings
{
    [Required]
    public string Key { get; set; }

    [Required]
    public string Issuer { get; set; }

    public string Audience { get; set; }
}
