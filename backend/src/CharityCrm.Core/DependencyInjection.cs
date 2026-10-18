using CharityCrm.Core.Services.Auth;
using CharityCrm.Core.Services.Users;
using Microsoft.Extensions.DependencyInjection;

namespace CharityCrm.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<UserService>();
        return services;
    }
}
