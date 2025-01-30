using AuthenticationProvider.Repositories;
using AuthenticationProvider.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Services.Tokens;
using AuthenticationProvider.Interfaces.Services;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using AuthenticationProvider.Repositories.Tokens;
using AuthenticationProvider.Services.Security;
using AuthenticationProvider.Services.Utilities;
using AuthenticationProvider.Interfaces.Tokens;
using AuthenticationProvider.Interfaces.Services.Security;
using AuthenticationProvider.Interfaces.Clients;
using AuthenticationProvider.Models.Data;
using AuthenticationProvider.Clients;

var builder = WebApplication.CreateBuilder(args);

// Add logging
builder.Logging.AddConsole();
builder.Logging.AddDebug();  

// Register DbContext with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register ASP.NET Core Identity services
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();  // Registering Identity services

// Register your application services
builder.Services.AddScoped<IAccountSecurityService, AccountSecurityService>();
builder.Services.AddScoped<IAccountVerificationTokenRepository, AccountVerificationTokenRepository>();
builder.Services.AddScoped<IAccountVerificationTokenService, AccountVerificationTokenService>();
builder.Services.AddScoped<IAccountVerificationService, AccountVerificationService>();
builder.Services.AddScoped<IAccountVerificationClient, AccountVerificationClient>();

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

builder.Services.AddScoped<IBusinessTypeService, BusinessTypeService>();
builder.Services.AddSingleton<IEmailRestrictionService, EmailRestrictionService>();


// Register HttpClient for ResetPasswordClient
builder.Services.AddHttpClient<ResetPasswordClient>(client =>
{
    var resetPasswordEndpoint = builder.Configuration.GetValue<string>("ResetPasswordProvider:Endpoint");
    client.BaseAddress = new Uri(resetPasswordEndpoint);
});


// Register HttpClient for AccountVerificationClient
builder.Services.AddHttpClient<AccountVerificationClient>(client =>
{
    var accountVerificationEndpoint = builder.Configuration.GetValue<string>("AccountVerificationProvider:Endpoint");
    client.BaseAddress = new Uri(accountVerificationEndpoint);
});
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ClockSkew = TimeSpan.Zero,

            // Custom claim validation for token type
            RoleClaimType = "TokenType"
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var tokenType = context.Principal?.FindFirst("TokenType")?.Value;

                // Example: Allow only specific token types
                if (tokenType != "Access" && tokenType != "ResetPassword" && tokenType != "AccountVerification")
                {
                    context.Fail("Invalid token type.");
                }

                return Task.CompletedTask;
            }
        };
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
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Start the application
app.Run();