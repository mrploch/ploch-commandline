---
name: winui3-testing
description: Testing strategies for WinUI 3 apps — unit testing ViewModels with xUnit/NUnit, WinUI unit test app template, UI automation with WinAppDriver/Appium, and mocking patterns.
invocable: false
---

# WinUI 3 Testing

## When to Use This Skill

Use when:
- Unit testing CommunityToolkit.Mvvm ViewModels
- Setting up a WinUI unit test project
- Writing UI automation tests with WinAppDriver
- Mocking services for ViewModel tests

## Architecture for Testability

**Key principle:** Keep ViewModels in a separate class library targeting plain `net10.0` (no `-windows` suffix). This enables testing with standard xUnit/NUnit without WinUI dependencies.

```
src/
  MyApp/                    # WinUI 3 app (net10.0-windows10.0.26100)
  MyApp.ViewModels/         # ViewModels library (net10.0) ← testable
  MyApp.Core/               # Domain logic (net10.0) ← testable
tests/
  MyApp.ViewModels.Tests/   # Standard xUnit project (net10.0)
```

## Unit Testing ViewModels

ViewModels inheriting from `ObservableObject` are plain .NET classes — fully testable without WinUI:

```csharp
public class ItemsViewModelTests
{
    private readonly Mock<IItemService> _mockService = new();
    private readonly ItemsViewModel _sut;

    public ItemsViewModelTests()
    {
        _sut = new ItemsViewModel(_mockService.Object);
    }

    [Fact]
    public async Task LoadAsync_PopulatesItems()
    {
        // Arrange
        var items = new List<Item> { new() { Name = "Test" } };
        _mockService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(items);

        // Act
        await _sut.LoadCommand.ExecuteAsync(null);

        // Assert
        Assert.Single(_sut.Items);
        Assert.Equal("Test", _sut.Items[0].Name);
    }

    [Fact]
    public void SaveCommand_DisabledWhenNameEmpty()
    {
        // Arrange
        _sut.Name = string.Empty;

        // Assert
        Assert.False(_sut.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void SaveCommand_EnabledWhenNameSet()
    {
        // Arrange
        _sut.Name = "Valid Name";

        // Assert
        Assert.True(_sut.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void PropertyChanged_RaisedOnNameChange()
    {
        // Arrange
        var raised = false;
        _sut.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ItemsViewModel.Name))
                raised = true;
        };

        // Act
        _sut.Name = "New Name";

        // Assert
        Assert.True(raised);
    }
}
```

## Testing AsyncRelayCommand

```csharp
[Fact]
public async Task LoadCommand_SetsIsRunningDuringExecution()
{
    // Arrange
    var tcs = new TaskCompletionSource<List<Item>>();
    _mockService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);

    // Act — start but don't complete
    var task = _sut.LoadCommand.ExecuteAsync(null);

    // Assert — IsRunning should be true while task is pending
    Assert.True(_sut.LoadCommand.IsRunning);

    // Complete the task
    tcs.SetResult(new List<Item>());
    await task;

    Assert.False(_sut.LoadCommand.IsRunning);
}
```

## Testing ObservableValidator

```csharp
[Fact]
public void Validation_FailsForEmptyRequiredField()
{
    // Arrange
    var vm = new RegistrationViewModel();

    // Act
    vm.Username = string.Empty;
    vm.ValidateAllProperties();

    // Assert
    Assert.True(vm.HasErrors);
    var errors = vm.GetErrors(nameof(vm.Username)).Cast<ValidationResult>().ToList();
    Assert.NotEmpty(errors);
}
```

## Testing WeakReferenceMessenger

```csharp
[Fact]
public async Task Delete_SendsItemDeletedMessage()
{
    // Arrange
    int? receivedId = null;
    WeakReferenceMessenger.Default.Register<ItemDeletedMessage>(this, (r, m) =>
    {
        receivedId = m.Value;
    });

    _sut.SelectedItem = new Item { Id = 42 };

    // Act
    await _sut.DeleteCommand.ExecuteAsync(null);

    // Assert
    Assert.Equal(42, receivedId);

    // Cleanup
    WeakReferenceMessenger.Default.UnregisterAll(this);
}
```

## WinUI Unit Test App (UI Thread Tests)

For tests that must run on the XAML UI thread (testing code that touches `Microsoft.UI.Xaml` types):

1. Use the "Unit Test App in Desktop (WinUI)" template in Visual Studio
2. Set the same TFM as the main project:

```xml
<TargetFramework>net10.0-windows10.0.26100.0</TargetFramework>
<WindowsAppSdkBootstrapInitialize>true</WindowsAppSdkBootstrapInitialize>
```

3. Use `[UITestMethod]` for tests requiring the UI thread:

```csharp
[UITestMethod]
public void Page_RendersCorrectly()
{
    var page = new MyPage();
    Assert.IsNotNull(page.Content);
}
```

## UI Automation with WinAppDriver

For end-to-end UI testing:

1. Install WinAppDriver from GitHub releases
2. Set `AutomationProperties.AutomationId` on key elements in XAML
3. Use Appium client to drive tests

```csharp
[TestClass]
public class MainWindowTests
{
    private static WindowsDriver<WindowsElement> _driver;

    [ClassInitialize]
    public static void Setup(TestContext context)
    {
        var options = new AppiumOptions();
        options.AddAdditionalCapability("app", @"path\to\app.exe");
        _driver = new WindowsDriver<WindowsElement>(
            new Uri("http://127.0.0.1:4723"), options);
    }

    [TestMethod]
    public void ClickSave_ShowsSuccessMessage()
    {
        var nameBox = _driver.FindElementByAccessibilityId("NameTextBox");
        nameBox.SendKeys("Test Item");

        var saveBtn = _driver.FindElementByAccessibilityId("SaveButton");
        saveBtn.Click();

        var info = _driver.FindElementByAccessibilityId("SuccessInfoBar");
        Assert.IsTrue(info.Displayed);
    }
}
```

**Note:** WinAppDriver maintenance status is unclear. For actively maintained alternatives, consider `YWinAppDriver` (community fork).
