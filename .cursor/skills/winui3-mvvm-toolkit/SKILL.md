---
name: winui3-mvvm-toolkit
description: Use CommunityToolkit.Mvvm source generators and patterns in WinUI 3 apps — ObservableProperty, RelayCommand, AsyncRelayCommand, ObservableValidator, WeakReferenceMessenger, and proper ViewModel design.
invocable: false
---

# WinUI 3 MVVM Toolkit Patterns

## When to Use This Skill

Use when:
- Writing ViewModels for WinUI 3 applications
- Using CommunityToolkit.Mvvm source generators
- Implementing form validation with ObservableValidator
- Setting up inter-ViewModel messaging
- Designing async command patterns with loading/cancellation

## Reference Files

- [source-generators.md](source-generators.md): Full attribute reference and generated code patterns
- [validation-and-messaging.md](validation-and-messaging.md): ObservableValidator, WeakReferenceMessenger patterns

## Core Rules

1. **All classes using source generators must be `partial`** — this is the #1 compilation error
2. **In WinUI 3, always inherit from `ObservableObject`** — do NOT use `[ObservableObject]` or `[INotifyPropertyChanged]` attributes (they are not AOT-compatible with WinRT/CsWinRT)
3. **`x:Bind` defaults to `OneTime`** — always specify `Mode=OneWay` or `Mode=TwoWay`

## ViewModel Base Pattern

```csharp
public partial class ItemViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string name = string.Empty;

    [ObservableProperty]
    private bool isLoading;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync(CancellationToken ct)
    {
        IsLoading = true;
        try
        {
            await _service.SaveAsync(Name, ct);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(Name);
}
```

## [ObservableProperty] Attribute

Annotate `private` fields — generator produces `public` properties with change notification:

```csharp
[ObservableProperty]
private string? title;           // -> public string? Title { get; set; }

[ObservableProperty]
private int count;               // -> public int Count { get; set; }
```

Field naming: `_title`, `m_title`, or `title` all generate `Title`. The generator strips `_` and `m_` prefixes.

### Change Hooks (Partial Methods)

Implement any combination — unimplemented partials have zero overhead:

```csharp
partial void OnTitleChanging(string? value);                    // before assignment
partial void OnTitleChanged(string? value);                     // after assignment
partial void OnTitleChanging(string? oldValue, string? newValue);
partial void OnTitleChanged(string? oldValue, string? newValue);
```

### Chaining Notifications

```csharp
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(FullName))]    // raises PropertyChanged for computed property
[NotifyCanExecuteChangedFor(nameof(SaveCommand))] // invalidates command's CanExecute
private string? firstName;

public string FullName => $"{FirstName} {LastName}";
```

### Forwarding Attributes

Use `[property:]` target to forward attributes to the generated property:

```csharp
[ObservableProperty]
[property: JsonPropertyName("user_name")]
[property: Required]
private string? username;
```

## [RelayCommand] Attribute

```csharp
// Synchronous command
[RelayCommand]
private void Reset() { }                          // -> ResetCommand : RelayCommand

// Async command
[RelayCommand]
private async Task LoadAsync(CancellationToken ct) { } // -> LoadCommand : AsyncRelayCommand

// With CanExecute
[RelayCommand(CanExecute = nameof(CanSave))]
private async Task SaveAsync() { }                 // -> SaveCommand (disabled when CanSave returns false)
private bool CanSave() => !string.IsNullOrWhiteSpace(Name);

// With cancel command
[RelayCommand(IncludeCancelCommand = true)]
private async Task FetchAsync(CancellationToken ct) { } // -> FetchCommand + CancelFetchCommand

// With parameter
[RelayCommand]
private void SelectItem(Item item) { }             // -> SelectItemCommand : RelayCommand<Item>
```

## AsyncRelayCommand Features

Bind to built-in properties for loading states:

```xml
<Button Content="Load" Command="{x:Bind ViewModel.LoadCommand}" />
<ProgressRing IsActive="{x:Bind ViewModel.LoadCommand.IsRunning, Mode=OneWay}" />
<Button Content="Cancel" Command="{x:Bind ViewModel.CancelFetchCommand}" />
```

## Error Handling in Async Commands

Handle exceptions inside the method body — `AsyncRelayCommand` does not swallow them:

```csharp
[RelayCommand]
private async Task LoadAsync(CancellationToken ct)
{
    try
    {
        ErrorMessage = null;
        Items = await _service.GetAsync(ct);
    }
    catch (OperationCanceledException) { /* normal cancellation */ }
    catch (Exception ex)
    {
        ErrorMessage = $"Failed to load: {ex.Message}";
        _logger.LogError(ex, "[ItemViewModel] Load failed");
    }
}
```
