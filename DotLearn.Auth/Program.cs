using DotLearn.Auth.Data;
using DotLearn.Auth.Middleware;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SecretsManager;
using Kralizek.Extensions.Configuration;
using DotLearn.Auth.Repositories;
using DotLearn.Auth.Services;
using DotLearn.Auth.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// AWS Secrets Manager (Only in non-Development environments)
if (!builder.Environment.IsDevelopment())
{
    // builder.Configuration.AddSecretsManager(region: Amazon.RegionEndpoint.APSoutheast2);
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseSqlServer(connStr));

builder.Services.AddHealthChecks().AddSqlServer(connStr, name: "sqlserver");

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Authentication & Authorization (Placeholder)
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// CORS — DOT-24 Security Lockdown
builder.Services.AddCors(options =>
{
    options.AddPolicy("DotLearnPolicy", policy =>
        policy.WithOrigins(
                "http://localhost:4200",
                "https://localhost:4200",
                builder.Configuration["AllowedOrigins:Ec2"] ?? "",
                builder.Configuration["AllowedOrigins:CloudFront"] ?? "")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

await DbSeeder.EnsurePopulatedAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Middlewares
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandler>();

app.UseCors("DotLearnPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
