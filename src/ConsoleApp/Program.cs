using ConsoleApp;
using ConsoleApp.Interfaces;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

var serviceCollection = new ServiceCollection()
        .AddInfrastructureServices()
        .AddConsoleAppServices()
        .AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
        })
    .BuildServiceProvider();

try
{
    var mediator = serviceCollection.GetService<IMediator>();

    if (mediator == null)
        throw new Exception("Unable to resolve mediator");

    var menu = serviceCollection.GetService<IMenu>();
    await menu!.PromptUserForSelection();
}
catch (Exception ex)
{
    Console.WriteLine($"FATAL ERROR: App will now close: {ex.Message}");
}

