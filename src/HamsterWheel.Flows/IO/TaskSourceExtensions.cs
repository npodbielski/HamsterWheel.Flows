using HamsterWheel.Flows.Blocks;
using HamsterWheel.HLinq.Data.Converters;

namespace HamsterWheel.Flows.IO;

public static class TaskSourceExtensions
{
    public static void SetSource<TIn, TOutput>(this ITaskSource<TIn> target, IPipelineBlock<TOutput> source,
        Func<TOutput, TIn> conversion)
    {
        if (source.Result.IsSingle)
        {
            target.SetSource(source.Result.SingleValue.ContinueWith(t => conversion(t.Result)));
        }
        else
        {
            target.SetSource(source.Result.AsEnumerable().Select(conversion));
        }
    }

    public static void SetSource<TIn, TOut>(this ITaskSource<TOut> taskSource, Task<TIn[]> multiSource,
        Func<TIn, TOut> conversion)
    {
        var task = MultiSource();
        taskSource.SetSource(task);
        return;

        async Task<IEnumerable<TOut>> MultiSource()
        {
            //conversion to object may lead to converting to JSON Node, which should not be the case. If the destination type is an object, conversion should not happen
            var finalConversion = typeof(TOut) != typeof(object)
                ? x => DefaultConverter.Instance.ConvertTo<TOut>(conversion(x)) ?? default(TOut)!
                : conversion;
            return (await multiSource).Select(finalConversion).Where(x => x != null).ToArray();
        }
    }
}