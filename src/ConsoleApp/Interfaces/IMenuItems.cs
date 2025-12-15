namespace ConsoleApp.Interfaces;

public interface IMenuItems
{
    Task HandleUniqueIpAddresses();
    Task HandleTopXMostActiveIps(int count = 3);
    Task HandleTopXVisitedUrls(int count = 3);
    void HandleExit();
}