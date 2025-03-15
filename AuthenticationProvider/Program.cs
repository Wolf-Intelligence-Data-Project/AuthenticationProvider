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
using Microsoft.AspNetCore.HttpOverrides;
using AuthenticationProvider.Interfaces.Services;
using AuthenticationProvider.Models.Data.Entities;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add logging
builder.Logging.AddConsole();
builder.Logging.AddDebug();  

// Register DbContext with SQL Server
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("UserDatabase")));

// Register ASP.NET Core Identity services
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<UserDbContext>()
    .AddDefaultTokenProviders();  // Registering Identity services

// Register your application services
builder.Services.AddScoped<IAccountSecurityService, AccountSecurityService>();
builder.Services.AddScoped<IAccountVerificationTokenRepository, AccountVerificationTokenRepository>();
builder.Services.AddScoped<IAccountVerificationTokenService, AccountVerificationTokenService>();
builder.Services.AddScoped<IAccountVerificationService, AccountVerificationService>();
builder.Services.AddScoped<IAccountVerificationClient, AccountVerificationClient>();
builder.Services.AddHttpClient<ICaptchaVerificationService, CaptchaVerificationService>();

builder.Services.AddScoped<IAccessTokenService, AccessTokenService>();
builder.Services.AddHostedService<AccessTokenCleanupService>();

builder.Services.AddScoped<ISignInService, SignInService>();
builder.Services.AddScoped<ISignUpService, SignUpService>();
builder.Services.AddScoped<ISignOutService, SignOutService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAddressRepository, AddressRepository>();


builder.Services.AddScoped<IResetPasswordTokenService, ResetPasswordTokenService>();
builder.Services.AddScoped<IResetPasswordTokenRepository, ResetPasswordTokenRepository>();
builder.Services.AddScoped<IResetPasswordService, ResetPasswordService>();
builder.Services.AddScoped<IResetPasswordClient, ResetPasswordClient>();

builder.Services.AddSingleton<IEmailRestrictionService, EmailRestrictionService>();
builder.Services.AddScoped<PasswordHasher<UserEntity>>();
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


// Access Token Validation
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
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
            ClockSkew = TimeSpan.Zero, // No clock skew for strict expiration checks
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.HttpContext.Request.Cookies["AccessToken"];
                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token; // Set the token from the cookie for validation
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var claimsIdentity = context.Principal.Identity as ClaimsIdentity;

                if (claimsIdentity == null)
                {
                    context.Fail("Token är ogiltigt. Claims identity är null."); // Swedish message
                    return Task.CompletedTask;
                }

                // Validate 'isVerified' claim
                var isVerifiedClaim = claimsIdentity.FindFirst("isVerified");

                if (isVerifiedClaim == null)
                {
                    context.Fail("Token är ogiltigt. Saknar 'isVerified' claim."); // Swedish message
                    return Task.CompletedTask;
                }

                if (isVerifiedClaim.Value != "true")
                {
                    context.Fail("Token är ogiltigt. Användaren är inte verifierad."); // Swedish message
                    return Task.CompletedTask;
                }

                // Log success after validation (log only non-sensitive data)
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Token validerades framgångsrikt för användare {UserName}.", claimsIdentity.Name);

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                // Log the error, including stack trace for better diagnostics
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError($"Autentisering misslyckades: {context.Exception.Message}"); // Swedish message
                if (context.Exception.StackTrace != null)
                {
                    logger.LogError(context.Exception.StackTrace);
                }

                return Task.CompletedTask;
            }
        };
    });

//builder.Services.AddAuthentication(options =>
//{
//    // Default scheme for access tokens
//    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//})

//// Access Token Authentication (Default)
//.AddJwtBearer("Bearer", options =>
//{
//    options.RequireHttpsMetadata = false;
//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuer = true,
//        ValidateAudience = true,
//        ValidateIssuerSigningKey = true,
//        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtAccess:Key"])),
//        ValidIssuer = builder.Configuration["JwtAccess:Issuer"],
//        ValidAudience = builder.Configuration["JwtAccess:Audience"],
//        ClockSkew = TimeSpan.Zero,
//        ValidateLifetime = true // Ensure token expiration is checked here as well
//    };

//    options.Events = new JwtBearerEvents
//    {
//        OnTokenValidated = context =>
//        {
//            var isVerifiedClaim = context.Principal?.FindFirst("isVerified")?.Value;
//            if (isVerifiedClaim != "true")
//            {
//                context.Fail("Kontot är inte verifierat.");
//            }
//            return Task.CompletedTask;
//        }
//    };
//})

//// Reset Password Token Authentication
//.AddJwtBearer("ResetPassword", options =>
//{
//    options.RequireHttpsMetadata = false;
//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuer = true,
//        ValidateAudience = true,
//        ValidateIssuerSigningKey = true,
//        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtResetPassword:Key"])),
//        ValidIssuer = builder.Configuration["JwtResetPassword:Issuer"],
//        ValidAudience = builder.Configuration["JwtResetPassword:Audience"],
//        ClockSkew = TimeSpan.Zero,
//    };

//    options.Events = new JwtBearerEvents
//    {
//        OnTokenValidated = context =>
//        {
//            var tokenType = context.Principal?.FindFirst("token_type")?.Value;
//            if (tokenType != "ResetPassword")
//            {
//                context.Fail("Invalid reset password token.");
//            }
//            return Task.CompletedTask;
//        }
//    };
//})

//// Account Verification Token Authentication
//.AddJwtBearer("AccountVerificationPolicy", options =>
//{
//    options.RequireHttpsMetadata = false;
//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuer = true,
//        ValidateAudience = true,
//        ValidateIssuerSigningKey = true,
//        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtVerification:Key"])),
//        ValidIssuer = builder.Configuration["JwtVerification:Issuer"],
//        ValidAudience = builder.Configuration["JwtVerification:Audience"],
//        ClockSkew = TimeSpan.Zero,
//    };

//    options.Events = new JwtBearerEvents
//    {
//        OnTokenValidated = context =>
//        {
//            var tokenType = context.Principal?.FindFirst("token_type")?.Value;
//            if (tokenType != "AccountVerification")
//            {
//                context.Fail("Invalid account verification token.");
//            }

//            return Task.CompletedTask;
//        },
//        OnAuthenticationFailed = context =>
//        {
//            context.Fail("Authentication failed: " + context.Exception.Message);
//            return Task.CompletedTask;
//        }
//    };
//});
//builder.Services.AddAuthorization(options =>
//{
//    options.AddPolicy("AccessTokenPolicy", policy =>
//        policy.RequireAuthenticatedUser().AddAuthenticationSchemes("Bearer"));

//    options.AddPolicy("ResetPasswordPolicy", policy =>
//        policy.RequireAuthenticatedUser().AddAuthenticationSchemes("ResetPassword"));

//    options.AddPolicy("AccountVerificationPolicy", policy =>
//        policy.RequireAuthenticatedUser().AddAuthenticationSchemes("AccountVerificationPolicy"));
//});



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

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
// Use CORS
app.UseCors("AllowAll");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Start the application
app.Run();