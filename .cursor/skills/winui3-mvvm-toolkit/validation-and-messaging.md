# Validation and Messaging Patterns

## ObservableValidator

Inherit from `ObservableValidator` (instead of `ObservableObject`) for form validation:

```csharp
public partial class RegistrationViewModel : ObservableValidator
{
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Username is required")]
    [MinLength(3, ErrorMessage = "Must be at least 3 characters")]
    [MaxLength(50)]
    private string username = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required]
    [EmailAddress(ErrorMessage = "Must be a valid email")]
    private string email = string.Empty;

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync()
    {
        ValidateAllProperties();  // run all validators before submit
        if (HasErrors) return;
        // proceed with submission
    }

    private bool CanSubmit() => !HasErrors;
}
```

### Displaying Validation Errors in XAML

**WinUI 3 does NOT have native `Validation.ErrorTemplate` support** (unlike WPF). You must manually expose error messages:

```csharp
// Helper method or per-property error properties
public string UsernameError => GetErrors(nameof(Username))
    ?.Cast<ValidationResult>()
    .FirstOrDefault()?.ErrorMessage ?? string.Empty;
```

```xml
<StackPanel>
    <TextBox Text="{x:Bind ViewModel.Username, Mode=TwoWay,
                    UpdateSourceTrigger=PropertyChanged}" />
    <TextBlock Text="{x:Bind ViewModel.UsernameError, Mode=OneWay}"
               Foreground="{ThemeResource SystemFillColorCriticalBrush}"
               FontSize="12" />
</StackPanel>
```

Alternative: use `InfoBar` control for form-level validation summaries.

### Cross-Property Validation

```csharp
[ObservableProperty]
[NotifyDataErrorInfo]
private int startYear;

partial void OnStartYearChanged(int value)
{
    ValidateProperty(EndYear, nameof(EndYear));  // re-validate dependent property
}
```

### Important: `HasErrors` Timing

`HasErrors` is only `true` after at least one validation has run. Call `ValidateAllProperties()` at form submission before checking `HasErrors`.

---

## WeakReferenceMessenger

Use for loose coupling between ViewModels (avoids strong references that cause memory leaks):

### Define a Message

```csharp
using CommunityToolkit.Mvvm.Messaging.Messages;

public class ItemDeletedMessage : ValueChangedMessage<int>
{
    public ItemDeletedMessage(int itemId) : base(itemId) { }
}

// For messages with no payload:
public class RefreshRequestedMessage { }
```

### Send a Message

```csharp
[RelayCommand]
private async Task DeleteAsync()
{
    await _service.DeleteAsync(SelectedItem.Id);
    WeakReferenceMessenger.Default.Send(new ItemDeletedMessage(SelectedItem.Id));
}
```

### Receive a Message

**Option 1: Lambda registration (in pages)**

```csharp
protected override void OnNavigatedTo(NavigationEventArgs e)
{
    WeakReferenceMessenger.Default.Register<ItemDeletedMessage>(this, (r, m) =>
    {
        if (Frame.CanGoBack) Frame.GoBack();
    });
}

protected override void OnNavigatedFrom(NavigationEventArgs e)
{
    WeakReferenceMessenger.Default.UnregisterAll(this);
}
```

**Option 2: IRecipient interface (in ViewModels)**

```csharp
public partial class ListViewModel : ObservableRecipient, IRecipient<ItemDeletedMessage>
{
    public void Receive(ItemDeletedMessage message)
    {
        Items.Remove(Items.FirstOrDefault(i => i.Id == message.Value));
    }

    // ObservableRecipient auto-registers when IsActive = true
}
```

### Memory Safety

- `WeakReferenceMessenger` uses weak references — no manual unsubscription needed if the recipient is garbage collected
- **Always unregister in pages** in `OnNavigatedFrom` to prevent stale handlers
- Prefer `WeakReferenceMessenger` over `StrongReferenceMessenger` unless profiling shows the weak reference overhead matters
