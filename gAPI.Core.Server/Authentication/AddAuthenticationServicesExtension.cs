using gAPI.Core.Dtos;
using gAPI.Core.Interfaces;
using gAPI.Core.Server.Entities;
using gAPI.Core.Server.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace gAPI.Core.Server.Authentication;

public static partial class AddAuthenticationServicesExtension
{
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services)
    {
        services.AddScoped<IServerAuthenticationService, EmptyServerAuthenticationService>();
        return services;
    }

    public static IServiceCollection AddAuthenticationServices<TUser, TStateDto>(this IServiceCollection services)
        where TUser : AuthUser, new()
        where TStateDto : AuthStateDto, new()
    {
        services.AddScoped<IAccountService, AccountService<TUser, TStateDto>>();

        services.AddScoped<ServerAuthenticationAccessor>();
        services.AddScoped<AuthenticationService<TUser, TStateDto>>();
        services.AddScoped(sp =>
        {
            var accessor = sp.GetRequiredService<ServerAuthenticationAccessor>();

            if (accessor.Current is null)
                return sp.GetRequiredService<AuthenticationService<TUser, TStateDto>>();

            return (accessor.Current as IAuthenticationService<TUser, TStateDto>)!;
        });
        services.AddScoped(sp =>
        {
            var accessor = sp.GetRequiredService<ServerAuthenticationAccessor>();

            if (accessor.Current is null)
                return sp.GetRequiredService<AuthenticationService<TUser, TStateDto>>();

            return (accessor.Current as IServerAuthenticationService)!;
        });
        services.AddAuthentication("gAPI")
                .AddScheme<AuthenticationSchemeOptions, AuthenticationHandler<TUser, TStateDto>>("gAPI", _ => { });
        services.AddScoped<IAuthenticationStateFactory<TUser, TStateDto>, AuthenticationStateFactory<TUser, TStateDto>>();
        services.AddScoped<IAuthenticationSecurity, AuthenticationSecurity<TUser, TStateDto>>();

        return services;
    }
}
