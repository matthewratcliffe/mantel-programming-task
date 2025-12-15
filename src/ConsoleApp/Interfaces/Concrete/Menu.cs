using Application.Interfaces;

namespace ConsoleApp.Interfaces.Concrete;

public class Menu(IMenuItems menuItems, IConsole console) : IMenu
{
    private readonly string[] _menuOptions =
    [
        "Unique IP Addresses",
        "Top 3 Visited Urls",
        "Top 3 Most Active IPs",
        "Exit"
    ];

    public virtual async Task PromptUserForSelection()
    {
        var selectedIndex = GetMenuSelection(_menuOptions, "Log Analysis Menu");

        if (selectedIndex > _menuOptions.Length - 1 || selectedIndex < 0)
        {
            console.WriteLine("Invalid option selected.");
            menuItems.HandleExit();

            return;
        }

        Console.Clear();
        switch (_menuOptions[selectedIndex])
        {
            case "Unique IP Addresses":
                await menuItems.HandleUniqueIpAddresses();
                break;
            case "Top 3 Visited Urls":
                await menuItems.HandleTopXVisitedUrls();
                break;
            case "Top 3 Most Active IPs":
                await menuItems.HandleTopXMostActiveIps();
                break;
            case "Exit":
                menuItems.HandleExit();
                break;
        }

        await PressAnyKeyToContinue();
    }

    protected virtual int GetMenuSelection(string[] options, string title)
    {
        var selectedIndex = 0;
        ConsoleKey keyPressed;

        do
        {
            console.Clear();
            DisplayMenu(options, selectedIndex, title);

            var keyInfo = console.ReadKey();
            keyPressed = keyInfo.Key;

            selectedIndex = keyPressed switch
            {
                // Update the selected index based on arrow keys
                ConsoleKey.UpArrow => selectedIndex == 0 ? options.Length - 1 : selectedIndex - 1,
                ConsoleKey.DownArrow => selectedIndex == options.Length - 1 ? 0 : selectedIndex + 1,
                _ => selectedIndex
            };
        } while (keyPressed != ConsoleKey.Enter);

        return selectedIndex;
    }

    protected virtual void DisplayMenu(string[] options, int selectedIndex, string title)
    {
        console.ForegroundColor = ConsoleColor.Cyan;
        console.WriteLine(title);
        console.WriteLine(new string('-', title.Length));
        console.ResetColor();
        console.WriteLine();

        // Display menu options
        for (var i = 0; i < options.Length; i++)
        {
            if (i == selectedIndex)
            {
                console.ForegroundColor = ConsoleColor.Black;
                console.BackgroundColor = ConsoleColor.White;
                console.WriteLine($"→ {options[i]}");
            }
            else
            {
                console.ForegroundColor = ConsoleColor.White;
                console.BackgroundColor = ConsoleColor.Black;
                console.WriteLine($"  {options[i]}");
            }

            console.ResetColor();
        }

        // Display navigation help
        console.WriteLine();
        console.WriteLine("Use arrow keys to navigate and Enter to select");
    }

    protected virtual async Task PressAnyKeyToContinue()
    {
        console.WriteLine();
        console.WriteLine("Press any key to continue...");
        console.ReadKey();
        await PromptUserForSelection();
    }
}