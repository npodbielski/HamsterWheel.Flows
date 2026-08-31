using HamsterWheel.HLinq.Data.Converters;

namespace HamsterWheel.Flows.Data.Serialization;

//A fallback converter that never converts. HLinq's DefaultConverter requires one in its constructor,
//but the HLinq packages do not ship an implementation - hosts can register their own instead
public sealed class DefaultFallbackConverter : IFallbackConverter
{
    public int Priority => int.MaxValue;
    public bool CanConvert(object? value, Type type) => false;
    public object? ConvertTo(object? value, Type type) => null;
}
