using System.Globalization;

namespace Ploch.CommandLine.Spectre.Serilog.Tests;

/// <summary>
///     Runs an action under a given current culture and restores the original afterwards. <see cref="CultureInfo.CurrentCulture" />
///     is per thread (it flows with the execution context), so changing it here does not leak into tests running in
///     parallel on other threads; the <c>finally</c> block restores it before the thread is reused.
/// </summary>
internal static class AmbientCulture
{
    public static void Run(string cultureName, Action action)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            action();
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }
}
