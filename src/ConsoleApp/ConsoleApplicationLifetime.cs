using Application.Interfaces;

namespace ConsoleApp;

public class ConsoleApplicationLifetime : IApplicationLifetime
{
    public void Exit()
    {
        Environment.Exit(0);
    }
}