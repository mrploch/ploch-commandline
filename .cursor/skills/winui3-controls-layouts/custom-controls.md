# Custom Controls in WinUI 3

## UserControl (Composite Controls)

For bundling existing WinUI controls together with custom DependencyProperties:

```xml
<!-- MyRatingControl.xaml -->
<UserControl x:Class="MyApp.Controls.MyRatingControl">
    <StackPanel Orientation="Horizontal">
        <TextBlock x:Name="LabelText" />
        <RatingControl Value="{x:Bind Value, Mode=TwoWay}" />
    </StackPanel>
</UserControl>
```

```csharp
public sealed partial class MyRatingControl : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(
            nameof(Label), typeof(string), typeof(MyRatingControl),
            new PropertyMetadata(string.Empty, OnLabelChanged));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value), typeof(double), typeof(MyRatingControl),
            new PropertyMetadata(0.0));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public MyRatingControl() => this.InitializeComponent();

    private static void OnLabelChanged(DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is MyRatingControl ctrl)
            ctrl.LabelText.Text = e.NewValue as string ?? string.Empty;
    }
}
```

## TemplatedControl (Full Re-Templating)

For controls where consumers need to re-template the visual tree:

```csharp
public sealed class BgLabelControl : Control
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(
            nameof(Label), typeof(string), typeof(BgLabelControl),
            new PropertyMetadata(string.Empty));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public BgLabelControl()
    {
        this.DefaultStyleKey = typeof(BgLabelControl);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        // Retrieve named parts from the template
        // var myButton = GetTemplateChild("PART_Button") as Button;
    }
}
```

### Default Style (MUST be in Themes/Generic.xaml)

```xml
<!-- Themes/Generic.xaml — file name and folder are hard-coded by XAML framework -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:local="using:MyApp.Controls">
    <Style TargetType="local:BgLabelControl">
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="local:BgLabelControl">
                    <Grid Background="{TemplateBinding Background}">
                        <TextBlock Text="{TemplateBinding Label}"
                                   HorizontalAlignment="Center"
                                   VerticalAlignment="Center" />
                    </Grid>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
</ResourceDictionary>
```

## DependencyProperty Pattern

```csharp
// Standard pattern for all DependencyProperties
public static readonly DependencyProperty MyPropertyProperty =
    DependencyProperty.Register(
        nameof(MyProperty),           // property name
        typeof(string),               // property type
        typeof(MyControl),            // owner type
        new PropertyMetadata(         // default value + optional change callback
            string.Empty,
            OnMyPropertyChanged));

public string MyProperty
{
    get => (string)GetValue(MyPropertyProperty);
    set => SetValue(MyPropertyProperty, value);
}

private static void OnMyPropertyChanged(DependencyObject d,
    DependencyPropertyChangedEventArgs e)
{
    if (d is MyControl ctrl)
    {
        // React to property changes
    }
}
```

## When to Use Which

| Scenario | Choice |
|----------|--------|
| Composite of existing controls | UserControl |
| Needs re-templating by consumers | TemplatedControl |
| Simple wrapper with properties | UserControl |
| Reusable library control | TemplatedControl |
| Quick and simple | UserControl |

## Key Rules

- `TemplateBinding` is read-only one-way binding. Use `Binding` with `RelativeSource={RelativeSource TemplatedParent}` for two-way.
- `Themes/Generic.xaml` path is hard-coded — must be exact name and location.
- `DefaultStyleKey` must be set in TemplatedControl constructor.
