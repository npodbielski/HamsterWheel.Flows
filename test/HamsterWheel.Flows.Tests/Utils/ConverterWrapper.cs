using HamsterWheel.HLinq.Data.Converters;

namespace HamsterWheel.Flows.Tests.Utils;

internal static class ConverterWrapper
{
    public static IDefaultConverter Converter { get; set; } = DefaultConverter.Instance;
}