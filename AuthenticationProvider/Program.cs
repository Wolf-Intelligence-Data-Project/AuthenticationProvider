using AuthenticationProvider.Data;
using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models;
using AuthenticationProvider.Models.Tokens;
using AuthenticationProvider.Repositories;
using AuthenticationProvider.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

// Add logging
builder.Logging.AddConsole();  // Log to the console
builder.Logging.AddDebug();    // Log to the debug output window

// Register DbContext with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Identity services
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Register services

// Register core services
builder.Services.AddScoped<ISignOutService, SignOutService>();
builder.Services.AddScoped<ISignInService, SignInService>();
builder.Services.AddScoped<ISignUpService, SignUpService>();

// Register repositories
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<IAddressRepository, AddressRepository>();
builder.Services.AddScoped<ILoginSessionTokenRepository, LoginSessionTokenRepository>();
builder.Services.AddScoped<IAccountVerificationTokenRepository, AccountVerificationTokenRepository>();

// Register token services
builder.Services.AddScoped<ILoginSessionTokenService, LoginSessionTokenService>();
builder.Services.AddScoped<IAccountVerificationTokenService, AccountVerificationTokenService>();

// Register email verification services
builder.Services.AddScoped<ISendVerificationService, SendVerificationService>();
builder.Services.AddSingleton<ISendVerificationClient, SendVerificationClient>();

// Register JwtSettings configuration
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

// Register HttpClient for the EmailVerificationClient
builder.Services.AddHttpClient<SendVerificationClient>(client =>
{
    var emailVerificationUrl = builder.Configuration.GetValue<string>("ExternalServices:EmailVerificationUrl");
    client.BaseAddress = new Uri(emailVerificationUrl);
});

// Add controllers
builder.Services.AddControllers();

// Add Swagger for API documentation
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
