using System.Globalization;
using System.Text.Json.Serialization;
using CharityCrm.Api.Setup;
using CharityCrm.Infrastructure;
using CharityCrm.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// Logging: structured JSON to stdout, level from configuration (NFR-OPS-04).
builder.Services.AddSerilog((services, config) => config
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console(new RenderedCompactJsonFormatter()));

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// Validation messages in German (UI language).
ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("de");
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddInfrastructure();

builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapHealthChecks("/api/health", new HealthCheckOptions { ResponseWriter = HealthCheckResponse.WriteAsync });

await DatabaseInitializer.InitializeAsync(app.Services);

await app.RunAsync();

public partial class Program;
