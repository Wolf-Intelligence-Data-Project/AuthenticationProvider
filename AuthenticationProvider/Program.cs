using AuthenticationProvider.Data;
using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models.Tokens;
using AuthenticationProvider.Repositories;
using AuthenticationProvider.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;  // Add this namespace
using AuthenticationProvider.Models;  // Make sure this namespace contains your ApplicationUser class

var builder = WebApplication.CreateBuilder(args);

// Add logging
builder.Logging.AddConsole();  // Log to the console
builder.Logging.AddDebug();    // Log to the debug output window

// Register DbContext with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register ASP.NET Core Identity services
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();  // Registering Identity services

// Register your application services
builder.Services.AddScoped<IAccountVerificationTokenRepository, AccountVerificationTokenRepository>();
builder.Services.AddScoped<IAccountVerificationTokenService, AccountVerificationTokenService>();
builder.Services.AddScoped<IAccountVerificationService, AccountVerificationService>();

builder.Services.AddScoped<IAccessTokenService, AccessTokenService>();

builder.Services.AddScoped<ISignInService, SignInService>();
builder.Services.AddScoped<ISignUpService, SignUpService>();
builder.Services.AddScoped<ISignOutService, SignOutService>();

builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<IAddressRepository, AddressRepository>();


builder.Services.AddScoped<IResetPasswordTokenService, ResetPasswordTokenService>();
builder.Services.AddScoped<IResetPasswordTokenRepository, ResetPasswordTokenRepository>();
builder.Services.AddScoped<IResetPasswordService, ResetPasswordService>();
builder.Services.AddScoped<IResetPasswordClient, ResetPasswordClient>();


builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));


// Register TokenRevocationService
builder.Services.AddScoped<ITokenRevocationService, TokenRevocationService>();

// Register HttpClient for ResetPasswordClient
builder.Services.AddHttpClient<ResetPasswordClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:7092/api/ResetPassword");
});

// Register HttpClient for EmailVerificationClient
builder.Services.AddHttpClient<AccountVerificationClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:7092/api/AccountVerification");
});

// Register IMemoryCache
builder.Services.AddMemoryCache();

// Register CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
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

// Use CORS
app.UseCors("AllowAll");

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Start the application
app.Run();
