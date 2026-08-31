using HamsterWheel.Data.Mapper;
using HamsterWheel.Flows.Blocks.Base;
using HamsterWheel.Utils;

namespace HamsterWheel.Flows.Blocks.Utils;

public class SetPropertyBlock : PipelineBlock<SetPropertyBlockInput, object, SetPropertyBlockTaskSource>
{
    public override Task<object> RunForInput(SetPropertyBlockInput input, CancellationToken token)
    {
        //if only the property path is provided just set new value with possible conversion if types are different
        if (!input.PropertyPath.IsNullOrWhiteSpace() && input.Map is null or { Count: <= 0 })
        {
            DataMapper.Map(input.Value, input.Object, [(".", input.PropertyPath)]);
        }
        //if the map is provided, use mapper
        else if (input.Map is { Count: > 0 })
        {
            //if both are provided than adjust map and run mapper anyway
            if (!input.PropertyPath.IsNullOrWhiteSpace())
            {
                for (var index = 0; index < input.Map.Count; index++)
                {
                    var mapping = input.Map[index];
                    input.Map[index] = mapping with
                    {
                        Destination = $"{input.PropertyPath}.{mapping.Destination}"
                    };
                }
            }

            //if value is null just skip mapping since properties names cannot be established
            if (input.Value is not null)
            {
                DataMapper.Map(input.Value, input.Object, [.. input.Map.Select(m => m)]);
            }
        }
        //if neither map nor property, run mapping of the entire object
        else
        {
            //if value is null just skip mapping since properties names cannot be established
            if (input.Value is not null)
            {
                DataMapper.Map(input.Value, input.Object);
            }
        }

        return Task.FromResult(input.Object);
    }
}