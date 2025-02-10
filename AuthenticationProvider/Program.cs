using AuthenticationProvider.Repositories;
using AuthenticationProvider.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AuthenticationProvider.Interfaces.Repositories;
using AuthenticationProvider.Services.Tokens;
using AuthenticationProvider.Interfaces.Utilities;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using AuthenticationProvider.Repositories.Tokens;
using AuthenticationProvider.Services.Security;
using AuthenticationProvider.Services.Utilities;
using AuthenticationProvider.Interfaces.Utilities.Security;
using AuthenticationProvider.Models.Data;
using AuthenticationProvider.Interfaces.Security;
using AuthenticationProvider.Interfaces.Services.Tokens;
using AuthenticationProvider.Interfaces.Services.Security.Clients;
using AuthenticationProvider.Services.Security.Clients;

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
builder.Services.AddHttpClient<ICaptchaVerificationService, CaptchaVerificationService>();


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

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})

// Access Token Authentication (Default)
.AddJwtBearer("Bearer", options =>
{
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtAccess:Key"])),
        ValidIssuer = builder.Configuration["JwtAccess:Issuer"],
        ValidAudience = builder.Configuration["JwtAccess:Audience"],
        ClockSkew = TimeSpan.Zero,
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            var isVerifiedClaim = context.Principal?.FindFirst("isVerified")?.Value;
            if (isVerifiedClaim != "true")
            {
                context.Fail("The account is not verified.");
            }
            return Task.CompletedTask;
        },
        OnMessageReceived = context =>
        {
            var token = context.HttpContext.Request.Cookies["AccessToken"];
            if (!string.IsNullOrEmpty(token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        }
    };
})

// Reset Password Token Authentication
.AddJwtBearer("ResetPassword", options =>
{
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtResetPassword:Key"])),
        ValidIssuer = builder.Configuration["JwtResetPassword:Issuer"],
        ValidAudience = builder.Configuration["JwtResetPassword:Audience"],
        ClockSkew = TimeSpan.Zero,
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            var tokenType = context.Principal?.FindFirst("token_type")?.Value;
            if (tokenType != "ResetPassword")
            {
                context.Fail("Invalid reset password token.");
            }
            return Task.CompletedTask;
        }
    };
})

// Account Verification Token Authentication
.AddJwtBearer("AccountVerification", options =>
{
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtVerification:Key"])),
        ValidIssuer = builder.Configuration["JwtVerification:Issuer"],
        ValidAudience = builder.Configuration["JwtVerification:Audience"],
        ClockSkew = TimeSpan.Zero,
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            var tokenType = context.Principal?.FindFirst("token_type")?.Value;
            if (tokenType != "AccountVerification")
            {
                context.Fail("Invalid account verification token.");
            }
            return Task.CompletedTask;
        }
    };
});

// Add Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAccessToken", policy =>
        policy.RequireAuthenticatedUser().AddAuthenticationSchemes("Bearer"));

    options.AddPolicy("RequireResetPasswordToken", policy =>
        policy.RequireAuthenticatedUser().AddAuthenticationSchemes("ResetPassword"));

    options.AddPolicy("RequireAccountVerificationToken", policy =>
        policy.RequireAuthenticatedUser().AddAuthenticationSchemes("AccountVerification"));
});


// HttpOnly cookie for access token
builder.Services.AddHttpContextAccessor();

// Register IMemoryCache
builder.Services.AddMemoryCache();

// Register CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
        builder.SetIsOriginAllowed(_ => true)
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials());
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