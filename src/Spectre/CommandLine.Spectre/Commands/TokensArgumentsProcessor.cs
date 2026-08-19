using System.Globalization;
using Ploch.Common;

namespace Ploch.CommandLine.Spectre.Commands;

/// <summary>
///     Substitutes date and time tokens into string command settings properties marked with
///     <see cref="SupportsTokensAttribute" />.
///     Recognised tokens are <c>{date}</c> and <c>{datetime}</c>, and both resolve in <b>UTC</b>, not local time.
/// </summary>
public class TokensArgumentsProcessor : CommandSettingsPropertyTypeProcessor<string>
{
    /// <summary>
    ///     Gets the attributes a property must carry to be processed — <see cref="SupportsTokensAttribute" />.
    /// </summary>
    public override Type[] RequiredAttributes => [ typeof(SupportsTokensAttribute) ];

    private static TokenInfo[] Tokens =>
    [ new("date",
          static () => DateTime.UtcNow.Date.ToString(DateTimeFormats.DateOnly.YearMonthDayNumbersWithDashes, CultureInfo.InvariantCulture),
          static () => DateTime.UtcNow.Date.ToString(DateTimeFormats.DateOnly.YearMonthDayNumbersWithDashes, CultureInfo.InvariantCulture)),
      new("datetime",
          static () => DateTime.UtcNow.ToString(DateTimeFormats.YearMonthDayHourMinuteSecondNumbersWithDashesAndColons, CultureInfo.InvariantCulture),
          static () => DateTime.UtcNow.ToString(DateTimeFormats.YearMonthDayHourMinuteSecondNumbersWithDashes, CultureInfo.InvariantCulture)) ];

    /// <summary>
    ///     Replaces every recognised <c>{token}</c> placeholder in the matched properties with its current value,
    ///     using the path-safe value when the property's <see cref="SupportsTokensAttribute.PathSafe" /> is set.
    /// </summary>
    protected override void DoProcessArguments()
    {
        foreach (var (_, property) in Properties)
        {
            var propertyValue = property.GetValue();
            if (propertyValue is null)
            {
                continue;
            }

            var attribute = GetAttribute<SupportsTokensAttribute>(property.PropertyInfo);
            foreach (var token in Tokens)
            {
                propertyValue = propertyValue.Replace($"{{{token.TokenName}}}",
                                                      attribute.PathSafe ? token.PathSafeValueProvider() : token.ValueProvider(),
                                                      StringComparison.OrdinalIgnoreCase);
            }

            property.SetValue(propertyValue);
        }
    }
}
