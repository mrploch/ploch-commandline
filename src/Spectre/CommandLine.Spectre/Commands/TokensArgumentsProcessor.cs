using System.Globalization;
using Ploch.Common;

namespace Ploch.CommandLine.Spectre.Commands;

public class TokensArgumentsProcessor : CommandSettingsPropertyTypeProcessor<string>
{
    public override Type[] RequiredAttributes => [ typeof(SupportsTokensAttribute) ];

    private static TokenInfo[] Tokens =>
    [ new("date",
          static () => DateTime.UtcNow.Date.ToString(DateTimeFormats.DateOnly.YearMonthDayNumbersWithDashes, CultureInfo.InvariantCulture),
          static () => DateTime.UtcNow.Date.ToString(DateTimeFormats.DateOnly.YearMonthDayNumbersWithDashes, CultureInfo.InvariantCulture)),
      new("datetime",
          static () => DateTime.UtcNow.ToString(DateTimeFormats.YearMonthDayHourMinuteSecondNumbersWithDashesAndColons, CultureInfo.InvariantCulture),
          static () => DateTime.UtcNow.ToString(DateTimeFormats.YearMonthDayHourMinuteSecondNumbersWithDashes, CultureInfo.InvariantCulture)) ];

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
