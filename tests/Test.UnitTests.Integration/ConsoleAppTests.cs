using System.Reflection;
using Application.Interfaces;
using ConsoleApp.Interfaces;
using ConsoleApp.Interfaces.Concrete;
using Moq;

namespace Test.UnitTests.Integration;

[TestFixture]
public class MenuIntegrationTests
{
    private Mock<IMenuItems> _mockMenuItems;
    private Mock<IConsole> _mockConsole;

    [SetUp]
    public void Setup()
    {
        _mockMenuItems = new Mock<IMenuItems>();
        _mockConsole = new Mock<IConsole>();
            
        // Setup default behavior for console to avoid null reference exceptions
        _mockConsole.Setup(c => c.Clear());
        _mockConsole.Setup(c => c.ResetColor());
        _mockConsole.Setup(c => c.WriteLine(It.IsAny<string>()));
    }

    [Test]
    public async Task IntegrationTest_InvalidMenuIndex_HandlesErrorCorrectly()
    {
        // Arrange
        // Set up a menu that will return an invalid index (out of bounds)
        var testMenu = new MenuWithInvalidSelection(_mockMenuItems.Object, _mockConsole.Object);
            
        // Act
        await testMenu.PromptUserForSelection();
            
        // Assert
        _mockConsole.Verify(c => c.WriteLine("Invalid option selected."), Times.Once);
        _mockMenuItems.Verify(m => m.HandleExit(), Times.Once);
            
        // Verify that PressAnyKeyToContinue is not called
        Assert.That(testMenu.PressAnyKeyToContinueCalled, Is.False);
    }

    [Test]
    public async Task IntegrationTest_ExitOptionSelected_CallsHandleExit()
    {
        // Arrange
        // Create the menu with a direct selection of the Exit option (index 3)
        var testMenu = new MenuWithFixedSelection(_mockMenuItems.Object, _mockConsole.Object, 3);
            
        // Act
        await testMenu.PromptUserForSelectionWithoutRecursion();
            
        // Assert
        _mockMenuItems.Verify(m => m.HandleExit(), Times.Once);
            
        // The actual real-world behavior is that HandleExit will call Environment.Exit(0)
        // which we can't test directly, but we can verify the call was made to our mock
    }

    [Test]
    public async Task IntegrationTest_ExitOptionSelected_NavigatedWithArrows()
    {
        // Arrange
        // Create a menu that will capture the last selected option
        var testMenu = new MenuWithNavigationTracking(_mockMenuItems.Object, _mockConsole.Object);
            
        // Set up the key sequence to navigate to the Exit option
        var keySequence = new Queue<ConsoleKeyInfo>();
            
        // Navigate to Exit (assuming it's the 4th option, index 3)
        // Press down arrow 3 times, then Enter
        keySequence.Enqueue(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
        keySequence.Enqueue(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
        keySequence.Enqueue(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
        keySequence.Enqueue(new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false));
            
        // Set up the console to return our key sequence
        _mockConsole.Setup(c => c.ReadKey(true))
            .Returns(() => keySequence.Count > 0 ? keySequence.Dequeue() : new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false));
            
        // Act
        await testMenu.ExecuteSingleMenuCycle();
            
        // Assert
        Assert.That(testMenu.LastSelectedOption, Is.EqualTo("Exit"));
        _mockMenuItems.Verify(m => m.HandleExit(), Times.Once);
    }
        
    [Test]
    public async Task IntegrationTest_PromptUserForSelection_IndexOutOfBounds_DirectlyReturnsInvalidValue()
    {
        // Arrange
        // Create a specialized menu that directly returns an out-of-bounds index from GetMenuSelection
        var testMenu = new MenuWithOutOfBoundsIndex(_mockMenuItems.Object, _mockConsole.Object);
            
        // Set up a key to press when "press any key to continue" is shown
        _mockConsole.Setup(c => c.ReadKey(true))
            .Returns(new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false));
                
        // Act
        await testMenu.PromptUserForSelection();
            
        // Assert
        _mockConsole.Verify(c => c.WriteLine("Invalid option selected."), Times.Once);
        _mockMenuItems.Verify(m => m.HandleExit(), Times.Once);
            
        // Verify we never called any of the handler methods for valid options
        _mockMenuItems.Verify(m => m.HandleUniqueIpAddresses(), Times.Never);
        _mockMenuItems.Verify(m => m.HandleTopXVisitedUrls(It.IsAny<int>()), Times.Never);
        _mockMenuItems.Verify(m => m.HandleTopXMostActiveIps(It.IsAny<int>()), Times.Never);
    }
        
    [Test]
    public async Task IntegrationTest_VerifyExitFromConsoleInput()
    {
        // Arrange
        // Create a menu that we'll use with the normal selection flow
        var testMenu = new MenuWithLimitedRecursion(_mockMenuItems.Object, _mockConsole.Object);
            
        // Set up the key sequence to select the option directly
        _mockConsole.SetupSequence(c => c.ReadKey(true))
            .Returns(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false))
            .Returns(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false))
            .Returns(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false))
            .Returns(new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false));
                
        // Act
        await testMenu.PromptUserForSelection();
            
        // Assert
        _mockMenuItems.Verify(m => m.HandleExit(), Times.Once);
    }
        
    [Test]
    public async Task IntegrationTest_VerifyUniqueIPSelectionFromConsoleInput()
    {
        // Arrange
        // Create a menu that we'll use with the normal selection flow
        var testMenu = new MenuWithLimitedRecursion(_mockMenuItems.Object, _mockConsole.Object);
            
        // Set up the key sequence to select the option directly
        _mockConsole.SetupSequence(c => c.ReadKey(true))
            .Returns(new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false));
                
        // Act
        await testMenu.PromptUserForSelection();
            
        // Assert
        _mockMenuItems.Verify(m => m.HandleUniqueIpAddresses(), Times.Once);
    }
        
    [Test]
    public async Task IntegrationTest_VerifyTop3VisitedUrlsFromConsoleInput()
    {
        // Arrange
        // Create a menu that we'll use with the normal selection flow
        var testMenu = new MenuWithLimitedRecursion(_mockMenuItems.Object, _mockConsole.Object);
            
        // Set up the key sequence to select the option directly
        _mockConsole.SetupSequence(c => c.ReadKey(true))
            .Returns(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false))
            .Returns(new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false));
                
        // Act
        await testMenu.PromptUserForSelection();
            
        // Assert
        _mockMenuItems.Verify(m => m.HandleTopXVisitedUrls(3), Times.Once);
    }
        
    [Test]
    public async Task IntegrationTest_VerifyTop3MostActiveFromConsoleInput()
    {
        // Arrange
        // Create a menu that we'll use with the normal selection flow
        var testMenu = new MenuWithLimitedRecursion(_mockMenuItems.Object, _mockConsole.Object);
            
        // Set up the key sequence to select the option directly
        _mockConsole.SetupSequence(c => c.ReadKey(true))
            .Returns(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false))
            .Returns(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false))
            .Returns(new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false));
                
        // Act
        await testMenu.PromptUserForSelection();
            
        // Assert
        _mockMenuItems.Verify(m => m.HandleTopXMostActiveIps(3), Times.Once);
    }
}

[TestFixture]
public class PressAnyKeyToContinueTests
{
    private Mock<IMenuItems> _mockMenuItems;
    private Mock<IConsole> _mockConsole;

    [SetUp]
    public void Setup()
    {
        _mockMenuItems = new Mock<IMenuItems>();
        _mockConsole = new Mock<IConsole>();
            
        _mockConsole.Setup(c => c.Clear());
        _mockConsole.Setup(c => c.ResetColor());
        _mockConsole.Setup(c => c.WriteLine(It.IsAny<string>()));
    }
        
    [Test]
    public async Task PressAnyKeyToContinue_CallsPromptUserForSelection_AfterKeyPress()
    {
        // Arrange
        _mockConsole.Setup(c => c.ReadKey(true))
            .Returns(new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false));
                
        var menu = new MenuWithRecursionTracking(_mockMenuItems.Object, _mockConsole.Object);
            
        // Act
        // Invoke the protected PressAnyKeyToContinue method using reflection
        var method = typeof(Menu).GetMethod("PressAnyKeyToContinue", 
            BindingFlags.NonPublic | BindingFlags.Instance);
                
        await (Task)method!.Invoke(menu, null)!;
            
        // Assert
        Assert.That(menu.PromptUserForSelectionCalled, Is.True, 
            "PromptUserForSelection should be called after key press");
        _mockConsole.Verify(c => c.WriteLine("Press any key to continue..."), Times.Once);
        _mockConsole.Verify(c => c.ReadKey(true), Times.Once);
    }
}
    
// Helper class for testing with an invalid selection
public class MenuWithInvalidSelection(IMenuItems menuItems, IConsole console) : Menu(menuItems, console)
{
    public bool PressAnyKeyToContinueCalled { get; private set; }

    // Override to return an invalid index
    protected override int GetMenuSelection(string[] options, string title)
    {
        return 999; // A value definitely outside the array bounds
    }
        
    // Override to track if this method is called
    protected override async Task PressAnyKeyToContinue()
    {
        PressAnyKeyToContinueCalled = true;
        await Task.CompletedTask; // Don't call base to avoid recursion
    }
}
    
// Helper class for testing with a fixed selection
public class MenuWithFixedSelection(IMenuItems menuItems, IConsole console, int selectionIndex)
    : Menu(menuItems, console)
{
    private readonly IConsole _consoleForTest = console;
    private readonly IMenuItems _menuItemsForTest = menuItems;

    // Override to return our fixed selection
    protected override int GetMenuSelection(string[] options, string title)
    {
        return selectionIndex;
    }
        
    // Non-recursive version for testing
    public async Task PromptUserForSelectionWithoutRecursion()
    {
        var menuOptionsField = typeof(Menu).GetField("_menuOptions", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        var menuOptions = (string[])menuOptionsField!.GetValue(this)!;

        if (selectionIndex > menuOptions.Length - 1)
        {
            _consoleForTest.WriteLine("Invalid option selected.");
            _menuItemsForTest.HandleExit();
            return;
        }
            
        // Skip Console.Clear() for testing
            
        switch (menuOptions[selectionIndex])
        {
            case "Unique IP Addresses":
                await _menuItemsForTest.HandleUniqueIpAddresses();
                break;
            case "Top 3 Visited Urls":
                await _menuItemsForTest.HandleTopXVisitedUrls();
                break;
            case "Top 3 Most Active IPs":
                await _menuItemsForTest.HandleTopXMostActiveIps();
                break;
            case "Exit":
                _menuItemsForTest.HandleExit();
                break;
        }
    }
        
    // Override to prevent recursion
    protected override async Task PressAnyKeyToContinue()
    {
        await Task.CompletedTask;
    }
}
    
// Helper class that tracks navigation
public class MenuWithNavigationTracking(IMenuItems menuItems, IConsole console) : Menu(menuItems, console)
{
    internal string? LastSelectedOption { get; private set; }
    private readonly IConsole _consoleForTest = console;
    private readonly IMenuItems _menuItemsForTest = menuItems;
    private bool _hasExecuted;

    // Execute just one menu cycle
    public async Task ExecuteSingleMenuCycle()
    {
        if (!_hasExecuted)
        {
            _hasExecuted = true;
            await PromptUserForSelection();
        }
    }
        
    // Override to track the selected option
    public override async Task PromptUserForSelection()
    {
        var menuOptionsField = typeof(Menu).GetField("_menuOptions", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        var menuOptions = (string[])menuOptionsField!.GetValue(this)!;
            
        var selectedIndex = GetMenuSelection(menuOptions, "Log Analysis Menu");
            
        if (selectedIndex > menuOptions.Length - 1)
        {
            _consoleForTest.WriteLine("Invalid option selected.");
            _menuItemsForTest.HandleExit();
            return;
        }
            
        // Track the selected option
        LastSelectedOption = menuOptions[selectedIndex];
            
        // Skip Console.Clear() for testing
            
        switch (menuOptions[selectedIndex])
        {
            case "Unique IP Addresses":
                await _menuItemsForTest.HandleUniqueIpAddresses();
                break;
            case "Top 3 Visited Urls":
                await _menuItemsForTest.HandleTopXVisitedUrls();
                break;
            case "Top 3 Most Active IPs":
                await _menuItemsForTest.HandleTopXMostActiveIps();
                break;
            case "Exit":
                _menuItemsForTest.HandleExit();
                break;
        }
            
        // Don't call PressAnyKeyToContinue to avoid recursion
    }
}
    
// Helper class that returns an out-of-bounds index
public class MenuWithOutOfBoundsIndex(IMenuItems menuItems, IConsole console) : Menu(menuItems, console)
{
    // Always return an index out of bounds
    protected override int GetMenuSelection(string[] options, string title)
    {
        return options.Length + 10; // Definitely out of bounds
    }
        
    // Override to prevent recursion
    protected override async Task PressAnyKeyToContinue()
    {
        await Task.CompletedTask;
    }
}
    
// Helper class with limited recursion
public class MenuWithLimitedRecursion(IMenuItems menuItems, IConsole console) : Menu(menuItems, console)
{
    private bool _hasExecuted;
    private readonly IConsole _consoleForTest = console;

    // Override to prevent infinite recursion
    protected override async Task PressAnyKeyToContinue()
    {
        if (_hasExecuted)
            return;
                
        _hasExecuted = true;
            
        // Call the actual console methods but avoid recursion
        _consoleForTest.WriteLine();
        _consoleForTest.WriteLine("Press any key to continue...");
        _consoleForTest.ReadKey();
            
        // Don't call PromptUserForSelection to avoid recursion
        await Task.CompletedTask;
    }
}
    
// Helper class for testing recursion
public class MenuWithRecursionTracking(IMenuItems menuItems, IConsole console) : Menu(menuItems, console)
{
    public bool PromptUserForSelectionCalled { get; private set; }

    public override async Task PromptUserForSelection()
    {
        PromptUserForSelectionCalled = true;
        await Task.CompletedTask;
    }
}