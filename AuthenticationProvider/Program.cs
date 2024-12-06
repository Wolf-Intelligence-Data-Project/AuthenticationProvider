using AuthenticationProvider.Data;
using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models.Tokens;
using AuthenticationProvider.Repositories;
using AuthenticationProvider.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

// Add logging
builder.Logging.AddConsole();  // Log to the console
builder.Logging.AddDebug();    // Log to the debug output window

// Register DbContext with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register services
builder.Services.AddScoped<ISignUpService, SignUpService>();
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>(); // Your implementation of ICompanyRepository
builder.Services.AddScoped<IAddressRepository, AddressRepository>();
builder.Services.AddScoped<IEmailVerificationService, EmailVerificationService>(); // EmailVerificationService that uses EmailVerificationClient
builder.Services.AddScoped<IResetPasswordTokenService, ResetPasswordTokenService>(); // Added ResetPasswordTokenService
builder.Services.AddScoped<IResetPasswordTokenRepository, ResetPasswordTokenRepository>(); // Add ResetPasswordTokenRepository
builder.Services.AddScoped<IResetPasswordService, ResetPasswordService>();
builder.Services.AddScoped<IResetPasswordClient, ResetPasswordClient>();
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<ITokenService, TokenService>();

// Register HttpClient for ResetPasswordClient
builder.Services.AddHttpClient<ResetPasswordClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:7092/api/EmailVerification"); // Base URL for ResetPassword API
});

// Register HttpClient for EmailVerificationClient (this is the missing part in your code)
builder.Services.AddHttpClient<EmailVerificationClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:7092/api/EmailVerification"); // Base URL for EmailVerification API
});

// Add controllers
builder.Services.AddControllers();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Enable Swagger UI in development environment
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Start the application
app.Run();
