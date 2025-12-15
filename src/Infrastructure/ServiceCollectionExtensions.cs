using Application.Interfaces;
using Infrastructure.Services.FileReader;
using Infrastructure.Services.LogParser;
using Infrastructure.Services.VirusScan;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds application services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services)
    {
        services.AddSingleton<IVirusScanServiceFactory>(_ =>
        {
            var apiKey = Environment.GetEnvironmentVariable("VIRUSTOTAL_API_KEY");
            return new VirusScanServiceFactory(apiKey);
        });
        
        services.AddScoped<ILogParser, RegexParserService>();
        services.AddScoped<IFileReader, FileReader>();
        
        return services;
    }
}