using CharityCrm.Core.Services.Auth;
using CharityCrm.Core.Services.Invoices;
using CharityCrm.Core.Services.Members;
using CharityCrm.Core.Services.Payments;
using CharityCrm.Core.Services.Settings;
using CharityCrm.Core.Services.Users;
using Microsoft.Extensions.DependencyInjection;

namespace CharityCrm.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<UserService>();
        services.AddScoped<MemberService>();
        services.AddScoped<SettingsService>();
        services.AddScoped<PaymentService>();
        services.AddScoped<InvoiceIssuer>();
        services.AddScoped<InvoiceService>();
        return services;
    }
}
