using gAPI.Dtos;
using gAPI.Interfaces;
using gAPI.Serializers;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace gAPI.Core.Client;

public static class AddAuthenticationServicesExtension
{
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, string apiAddress)
    {
        services.AddScoped<IStateParser<AuthStateDto>, DefaultStateParser>();
        return services.AddAuthenticationServices<AuthStateDto>(apiAddress);
    }
    public static IServiceCollection AddAuthenticationServices<TStateDto>(this IServiceCollection services, string apiAddress)
        where TStateDto : AuthStateDto, new()
    {
        // Register the cookie handler
        services.AddScoped<WithCookiesHandler>();

        // Configure named client for WebAssembly with cookies
        services
            .AddHttpClient("WithCookies", opt => opt.BaseAddress = new Uri(apiAddress)) 
            .AddHttpMessageHandler<WithCookiesHandler>();

        // Register global client authentication service
        services.AddScoped<AuthenticatedHttpClient<TStateDto>>();
        services.AddScoped<IAuthenticatedHttpClient<TStateDto>>(sp => sp.GetRequiredService<AuthenticatedHttpClient<TStateDto>>());
        services.AddScoped<gAPI.Interfaces.IClientAuthenticatedHttpClient>(sp => sp.GetRequiredService<AuthenticatedHttpClient<TStateDto>>());
        services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<AuthenticatedHttpClient<TStateDto>>());

        // Set up authorization core
        services.AddAuthorizationCore();

        return services;
    }
}
