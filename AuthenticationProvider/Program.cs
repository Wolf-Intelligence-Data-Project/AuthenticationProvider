using AuthenticationProvider.Data;
using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Models;
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
builder.Services.AddScoped<IEmailVerificationService, EmailVerificationService>(); // Placeholder implementation, will connect to EmailVerificationProvider service later
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<ITokenService, TokenService>();

// Register HttpClient for the EmailVerificationClient
builder.Services.AddHttpClient<EmailVerificationClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:7092/api/SendVerificationEmail"); // Change if necessary
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
