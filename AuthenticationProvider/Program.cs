using AuthenticationProvider.Interfaces;
using AuthenticationProvider.Repositories;
using AuthenticationProvider.Services;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddScoped<ISignUpService, SignUpService>();
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>(); // Your implementation of ICompanyRepository
builder.Services.AddScoped<IAddressRepository, AddressRepository>();
builder.Services.AddScoped<ITokenProvider, TokenProvider>(); // Placeholder implementation, will connect to TokenProvider service later
builder.Services.AddScoped<IEmailVerificationProvider, EmailVerificationProvider>(); // Placeholder implementation, will connect to EmailVerificationProvider service later

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
app.Run();
