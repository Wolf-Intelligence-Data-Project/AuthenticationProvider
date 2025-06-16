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
using AuthenticationProvider.Services.Utilities;
using AuthenticationProvider.Interfaces.Utilities.Security;
using AuthenticationProvider.Models.Data;
using AuthenticationProvider.Interfaces.Security;
using AuthenticationProvider.Interfaces.Services.Tokens;
using AuthenticationProvider.Interfaces.Services.Security.Clients;
using Microsoft.AspNetCore.HttpOverrides;
using AuthenticationProvider.Interfaces.Services;
using AuthenticationProvider.Models.Data.Entities;
using System.Security.Claims;
using AuthenticationProvider.Services.Clients;
using AuthenticationProvider.Services.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();
builder.Logging.AddDebug();  

builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("UserDatabase")));

builder.Services.AddIdentityCore<UserEntity>()
    .AddEntityFrameworkStores<UserDbContext>()
    .AddDefaultTokenProviders();

// Register your application services
builder.Services.AddScoped<IEmailSecurityService, EmailSecurityService>();
builder.Services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
builder.Services.AddScoped<IEmailVerificationTokenService, EmailVerificationTokenService>();
builder.Services.AddScoped<IEmailVerificationService, EmailVerificationService>();
builder.Services.AddScoped<IEmailVerificationClient, EmailVerificationClient>();
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

builder.Services.AddHttpClient<ResetPasswordClient>(client =>
{
    var resetPasswordEndpoint = builder.Configuration.GetValue<string>("ResetPasswordProvider:Endpoint");
    client.BaseAddress = new Uri(resetPasswordEndpoint);
});

builder.Services.AddHttpClient<EmailVerificationClient>(client =>
{
    var emailVerificationEndpoint = builder.Configuration.GetValue<string>("EmailVerificationProvider:Endpoint");
    client.BaseAddress = new Uri(emailVerificationEndpoint);
});

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
            ClockSkew = TimeSpan.Zero, 
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.HttpContext.Request.Cookies["AccessToken"];
                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token; 
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var claimsIdentity = context.Principal.Identity as ClaimsIdentity;

                if (claimsIdentity == null)
                {
                    context.Fail("Token är ogiltigt. Claims identity är null.");
                    return Task.CompletedTask;
                }
                var isVerifiedClaim = claimsIdentity.FindFirst("isVerified");

                if (isVerifiedClaim == null)
                {
                    context.Fail("Token är ogiltigt. Saknar 'isVerified' claim.");
                    return Task.CompletedTask;
                }

                if (isVerifiedClaim.Value != "true")
                {
                    context.Fail("Token är ogiltigt. Användaren är inte verifierad.");
                    return Task.CompletedTask;
                }

                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Token validerades framgångsrikt för användare {UserName}.", claimsIdentity.Name);

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError($"Autentisering misslyckades: {context.Exception.Message}");
                if (context.Exception.StackTrace != null)
                {
                    logger.LogError(context.Exception.StackTrace);
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddHttpContextAccessor();

builder.Services.AddMemoryCache();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
        builder.SetIsOriginAllowed(_ => true)
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials());
});

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseCors("AllowAll");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();