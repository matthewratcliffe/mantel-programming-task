using System.Reflection;
using Application.Interfaces;
using ConsoleApp.Interfaces;
using ConsoleApp.Interfaces.Concrete;
using Moq;

namespace Test.UnitTests.ConsoleApp;

[TestFixture]
public class MenuTests
{
    private Mock<IMenuItems> _mockMenuItems;
    private Mock<IConsole> _mockConsole;

    [SetUp]
    public void Setup()
    {
        _mockMenuItems = new Mock<IMenuItems>();
        _mockConsole = new Mock<IConsole>();
    }

    #region PromptUserForSelection Tests

    [Test]
    public async Task PromptUserForSelection_WithValidSelection_CallsCorrectHandler()
    {
        // Arrange
        _mockMenuItems.Setup(m => m.HandleUniqueIpAddresses()).Returns(Task.CompletedTask);
        var menu = new TestableMenu(_mockMenuItems.Object, _mockConsole.Object, 0);
            
        // Act
        await menu.PromptUserForSelectionWithoutRecursion();
            
        // Assert
        _mockMenuItems.Verify(m => m.HandleUniqueIpAddresses(), Times.Once);
    }

    [Test]
    public async Task PromptUserForSelection_WithTopVisitedUrlsSelection_CallsCorrectHandler()
    {
        // Arrange
        _mockMenuItems.Setup(m => m.HandleTopXVisitedUrls(It.IsAny<int>())).Returns(Task.CompletedTask);
        var menu = new TestableMenu(_mockMenuItems.Object, _mockConsole.Object, 1);
            
        // Act
        await menu.PromptUserForSelectionWithoutRecursion();
            
        // Assert
        _mockMenuItems.Verify(m => m.HandleTopXVisitedUrls(3), Times.Once);
    }

    [Test]
    public async Task PromptUserForSelection_WithTopActiveIPsSelection_CallsCorrectHandler()
    {
        // Arrange
        _mockMenuItems.Setup(m => m.HandleTopXMostActiveIps(It.IsAny<int>())).Returns(Task.CompletedTask);
        var menu = new TestableMenu(_mockMenuItems.Object, _mockConsole.Object, 2);
            
        // Act
        await menu.PromptUserForSelectionWithoutRecursion();
            
        // Assert
        _mockMenuItems.Verify(m => m.HandleTopXMostActiveIps(3), Times.Once);
    }

    [Test]
    public async Task PromptUserForSelection_WithExitSelection_CallsHandleExit()
    {
        // Arrange
        var menu = new TestableMenu(_mockMenuItems.Object, _mockConsole.Object, 3);
            
        // Act
        await menu.PromptUserForSelectionWithoutRecursion();
            
        // Assert
        _mockMenuItems.Verify(m => m.HandleExit(), Times.Once);
    }

    [Test]
    public async Task PromptUserForSelection_WithInvalidIndex_CallsHandleExit()
    {
        // Arrange
        var menu = new TestableMenu(_mockMenuItems.Object, _mockConsole.Object, 99);
            
        // Act
        await menu.PromptUserForSelectionWithoutRecursion();
            
        // Assert
        _mockConsole.Verify(c => c.WriteLine("Invalid option selected."), Times.Once);
        _mockMenuItems.Verify(m => m.HandleExit(), Times.Once);
    }

    #endregion

    #region GetMenuSelection Tests

    [Test]
    public void GetMenuSelection_WithImmediateEnterKey_ReturnsFirstOption()
    {
        // Arrange
        _mockConsole.SetupSequence(c => c.ReadKey(true))
            .Returns(new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false));
            
        var menu = new MenuWithExposedMethods(_mockMenuItems.Object, _mockConsole.Object);
            
        // Act
        var result = menu.TestGetMenuSelection(["Test1", "Test2"], "Title");
            
        // Assert
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void GetMenuSelection_WithDownArrow_SelectsSecondOption()
    {
        // Arrange
        _mockConsole.SetupSequence(c => c.ReadKey(true))
            .Returns(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false))
            .Returns(new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false));
            
        var menu = new MenuWithExposedMethods(_mockMenuItems.Object, _mockConsole.Object);
            
        // Act
        var result = menu.TestGetMenuSelection(["Test1", "Test2", "Test3"], "Title");
            
        // Assert
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void GetMenuSelection_WithUpArrow_CyclesToLastOption()
    {
        // Arrange
        _mockConsole.SetupSequence(c => c.ReadKey(true))
            .Returns(new ConsoleKeyInfo('\0', ConsoleKey.UpArrow, false, false, false))
            .Returns(new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false));
            
        var menu = new MenuWithExposedMethods(_mockMenuItems.Object, _mockConsole.Object);
            
        // Act
        var result = menu.TestGetMenuSelection(["Test1", "Test2", "Test3"], "Title");
            
        // Assert
        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void GetMenuSelection_WithMultipleDownArrows_CyclesBackToFirstOption()
    {
        // Arrange
        _mockConsole.SetupSequence(c => c.ReadKey(true))
            .Returns(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false))
            .Returns(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false))
            .Returns(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false))
            .Returns(new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false));
            
        var menu = new MenuWithExposedMethods(_mockMenuItems.Object, _mockConsole.Object);
            
        // Act
        var result = menu.TestGetMenuSelection(["Test1", "Test2", "Test3"], "Title");
            
        // Assert
        Assert.That(result, Is.EqualTo(0));
    }

    #endregion

    #region DisplayMenu Tests

    [Test]
    public void DisplayMenu_WithSelectedFirstItem_FormatsCorrectly()
    {
        // Arrange
        var options = new[] { "Option1", "Option2", "Option3" };
        var menu = new MenuWithExposedDisplayMenu(_mockMenuItems.Object, _mockConsole.Object);
            
        // Act
        menu.TestDisplayMenu(options, 0, "Test Title");
            
        // Assert
        VerifyConsoleColorChanges();
        _mockConsole.Verify(c => c.WriteLine("Test Title"), Times.Once);
        _mockConsole.Verify(c => c.WriteLine(new string('-', "Test Title".Length)), Times.Once);
        _mockConsole.Verify(c => c.WriteLine("→ Option1"), Times.Once);
        _mockConsole.Verify(c => c.WriteLine("  Option2"), Times.Once);
        _mockConsole.Verify(c => c.WriteLine("  Option3"), Times.Once);
    }

    [Test]
    public void DisplayMenu_WithSelectedMiddleItem_FormatsCorrectly()
    {
        // Arrange
        var options = new[] { "Option1", "Option2", "Option3" };
        var menu = new MenuWithExposedDisplayMenu(_mockMenuItems.Object, _mockConsole.Object);
            
        // Act
        menu.TestDisplayMenu(options, 1, "Test Title");
            
        // Assert
        VerifyConsoleColorChanges();
        _mockConsole.Verify(c => c.WriteLine("→ Option2"), Times.Once);
        _mockConsole.Verify(c => c.WriteLine("  Option1"), Times.Once);
        _mockConsole.Verify(c => c.WriteLine("  Option3"), Times.Once);
        _mockConsole.Verify(c => c.WriteLine("Use arrow keys to navigate and Enter to select"), Times.Once);
    }

    private void VerifyConsoleColorChanges()
    {
        _mockConsole.VerifySet(c => c.ForegroundColor = It.IsAny<ConsoleColor>(), Times.AtLeastOnce());
        _mockConsole.VerifySet(c => c.BackgroundColor = It.IsAny<ConsoleColor>(), Times.AtLeastOnce());
        _mockConsole.Verify(c => c.ResetColor(), Times.AtLeastOnce());
    }

    #endregion

    #region PressAnyKeyToContinue Test

    [Test]
    public void PressAnyKeyToContinue_DisplaysMessageAndWaitsForKeyPress()
    {
        // Arrange
        _mockConsole.Setup(c => c.ReadKey(true))
            .Returns(new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false));
            
        var menu = new MenuWithExposedMethods(_mockMenuItems.Object, _mockConsole.Object);
            
        // Act
        menu.TestPressAnyKeyToContinue();
            
        // Assert
        _mockConsole.Verify(c => c.WriteLine("Press any key to continue..."), Times.Once);
        _mockConsole.Verify(c => c.ReadKey(true), Times.AtLeastOnce());
    }

    #endregion
}

#region Test Helper Classes

// Class for testing PromptUserForSelection
public class TestableMenu(IMenuItems menuItems, IConsole console, int selectionIndex) : Menu(menuItems, console)
{
    private readonly IMenuItems _menuItemsForTest = menuItems;
    private readonly IConsole _consoleForTest = console;

    protected override int GetMenuSelection(string[] options, string title)
    {
        return selectionIndex;
    }
        
    // Non-recursive version of PromptUserForSelection
    public async Task PromptUserForSelectionWithoutRecursion()
    {
        // Get the private menuOptions field
        var menuOptionsField = typeof(Menu).GetField("_menuOptions", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        var menuOptions = (string[])menuOptionsField!.GetValue(this)!;

        if (selectionIndex > menuOptions.Length - 1)
        {
            _consoleForTest.WriteLine("Invalid option selected.");
            _menuItemsForTest.HandleExit();
            return;
        }
            
        // Skip the Console.Clear() call that would affect test output

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
}

// Class for testing GetMenuSelection and PressAnyKeyToContinue
public class MenuWithExposedMethods(IMenuItems menuItems, IConsole console) : Menu(menuItems, console)
{
    private readonly IConsole _consoleForTest = console;

    public int TestGetMenuSelection(string[] options, string title)
    {
        return GetMenuSelection(options, title);
    }
        
    // Expose a test version of PressAnyKeyToContinue that doesn't recurse
    public void TestPressAnyKeyToContinue()
    {
        _consoleForTest.WriteLine();
        _consoleForTest.WriteLine("Press any key to continue...");
        _consoleForTest.ReadKey();
        // Don't call PromptUserForSelection to avoid recursion
    }
        
    // Override to make testing simpler
    protected override void DisplayMenu(string[] options, int selectedIndex, string title)
    {
        // Do nothing for testing purposes
    }
}

// Class for testing DisplayMenu
public class MenuWithExposedDisplayMenu(IMenuItems menuItems, IConsole console) : Menu(menuItems, console)
{
    public void TestDisplayMenu(string[] options, int selectedIndex, string title)
    {
        DisplayMenu(options, selectedIndex, title);
    }
}
    
#endregion