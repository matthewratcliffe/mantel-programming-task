using Application.Interfaces;
using ConsoleApp.Interfaces;
using ConsoleApp.Interfaces.Concrete;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleApp;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds application services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddConsoleAppServices(
        this IServiceCollection services)
    {
        services.AddSingleton<IConsole, SystemConsole>();
        services.AddScoped<IMenu, Menu>();
        services.AddScoped<IMenuItems, MenuItems>();
        services.AddScoped<IApplicationLifetime, ConsoleApplicationLifetime>();
        
        return services;
    }
}