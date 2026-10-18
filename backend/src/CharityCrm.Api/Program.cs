using System.Globalization;
using System.Text.Json.Serialization;
using CharityCrm.Api.Features.Auth;
using CharityCrm.Api.Features.Finance;
using CharityCrm.Api.Features.Members;
using CharityCrm.Api.Features.Reports;
using CharityCrm.Api.Features.Users;
using CharityCrm.Api.Setup;
using CharityCrm.Core;
using CharityCrm.Core.Common;
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
// Error keys in camelCase so the SPA can map them to form fields.
ValidatorOptions.Global.PropertyNameResolver = (_, member, _) => member?.Name.ToCamelCase();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddValidatorsFromAssemblyContaining<CharityCrm.Core.Validation.LoginRequestValidator>();

builder.Services.AddCore();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAppSecurity(builder.Configuration);

builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseAuthentication();
app.UseMiddleware<AntiforgeryValidationMiddleware>();
app.UseMiddleware<PasswordChangeRequiredMiddleware>();
app.UseRateLimiter();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

app.MapHealthChecks("/api/health", new HealthCheckOptions { ResponseWriter = HealthCheckResponse.WriteAsync })
    .AllowAnonymous();
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapMemberEndpoints();
app.MapSettingsEndpoints();
app.MapPaymentEndpoints();
app.MapInvoiceEndpoints();
app.MapReportEndpoints();

await DatabaseInitializer.InitializeAsync(app.Services);

// Optional development command: dotnet CharityCrm.Api.dll seed-demo [members] [payments]
if (args is ["seed-demo", ..])
{
    var memberCount = args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : 5_000;
    var paymentCount = args.Length > 2 ? int.Parse(args[2], CultureInfo.InvariantCulture) : 100_000;
    await DemoDataSeeder.SeedAsync(app.Services, memberCount, paymentCount);
    return;
}

await app.RunAsync();

public partial class Program;
