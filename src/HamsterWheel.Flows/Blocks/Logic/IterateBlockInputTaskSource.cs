using System.Collections;
using HamsterWheel.Flows.IO;
using HamsterWheel.HLinq.Data.Converters;

namespace HamsterWheel.Flows.Blocks.Logic;

public class IterateBlockInputTaskSource : SingleInputTaskSource<object>
{
    /// <summary>
    /// By the definition iteration never returns single value so this needs to be set to false
    /// </summary>
    public override bool AllSingle => false;

    // ReSharper disable once UnusedMember.Global; Reason: it is used inside generated code
    /// <summary>
    /// Iterate block should not distinguish between Single or Multi data inputs. All should be iterated and spit out.
    /// This method makes sure that all the blocks that returns i.e. array of items when linked to <see cref="IterateBlock"/> will cause it to concatenate all the items and return them one by one.
    /// <example>
    /// Block A returns [1,2,3]
    /// Block B returns [4,5,6,7]
    /// <see cref="IterateBlock"/> should output collection of [1,2,3,4,5,6,7]
    /// </example>
    /// </summary>
    /// <param name="items"></param>
    /// <typeparam name="TItem"></typeparam>
    public void SetSource<TItem>(Task<TItem[]> items)
    {
        base.SetSource(Function());
        return;

        async Task<IEnumerable<IEnumerable<TItem>>> Function()
        {
            var enumerable = await items;
            //in case the other block returned [[1,2,3,4,5]] as its result, expected behavior is to flatten the array and return [[1],[2],[3],[4],[5]] as result
            if (enumerable.Length == 1
                && enumerable.First() is IEnumerable nestedEnumerable
                && nestedEnumerable.OfType<object>().Any())
            {
                return [nestedEnumerable.OfType<object>().Select(DefaultConverter.Instance.ConvertTo<TItem>)];
            }

            return [enumerable];
        }
    }
}