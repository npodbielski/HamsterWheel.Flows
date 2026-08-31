using Cronos;

namespace HamsterWheel.Flows;

public static class StringCronExtensions
{
    //Cronos' default format is Standard (5 fields, no seconds). IncludeSeconds additionally accepts 6-field expressions
    public static bool IsCronExpression(this string? str) =>
        str != null &&
        (CronExpression.TryParse(str, out _) || CronExpression.TryParse(str, CronFormat.IncludeSeconds, out _));

    public static CronExpression ParseCronExpression(this string str) =>
        CronExpression.TryParse(str, out var standard)
            ? standard
            : CronExpression.Parse(str, CronFormat.IncludeSeconds);
}