# MVVM Toolkit Source Generators Reference

## Generated Code Examples

### [ObservableProperty] generates:

```csharp
// Input:
[ObservableProperty]
private string? name;

// Generated:
public string? Name
{
    get => name;
    set
    {
        if (!EqualityComparer<string?>.Default.Equals(name, value))
        {
            OnNameChanging(value);
            OnNameChanging(name, value);
            OnPropertyChanging(global::CommunityToolkit.Mvvm.ComponentModel.__Internals.Name);
            name = value;
            OnNameChanged(value);
            OnNameChanged(name, value);
            OnPropertyChanged(global::CommunityToolkit.Mvvm.ComponentModel.__Internals.Name);
        }
    }
}
```

### [RelayCommand] generates:

```csharp
// Input:
[RelayCommand]
private async Task GreetUserAsync() { }

// Generated:
private AsyncRelayCommand? greetUserCommand;
public IAsyncRelayCommand GreetUserCommand =>
    greetUserCommand ??= new AsyncRelayCommand(GreetUserAsync);
```

## Naming Convention

| Method Name | Generated Property |
|------------|-------------------|
| `Save()` | `SaveCommand` |
| `SaveAsync()` | `SaveCommand` (not SaveAsyncCommand) |
| `LoadDataAsync(CancellationToken ct)` | `LoadDataCommand` |
| `OnItemSelected(Item item)` | `OnItemSelectedCommand` |

The `Async` suffix is stripped. `CancellationToken` parameters are handled automatically.

## Common Compilation Errors

| Error | Cause | Fix |
|-------|-------|-----|
| `CS0260` | Missing `partial` on class | Add `partial` to class declaration |
| `MVVMTK0049` | Using `[INotifyPropertyChanged]` attribute | Inherit from `ObservableObject` instead |
| `MVVMTK0050` | Using `[ObservableObject]` attribute | Inherit from `ObservableObject` instead |
| `CS0103` (property not found) | Field not in a partial class | Ensure all parent types are also `partial` |
