using HamsterWheel.Utils;

namespace HamsterWheel.Flows.Blocks.Utils;

public class CreateObjectInput
{
    public string Property1
    {
        get;
        set => field = value.ToPascalCase();
    } = null!;

    public string? Property2
    {
        get;
        set => field = value?.ToPascalCase();
    }

    public string? Property3
    {
        get;
        set => field = value?.ToPascalCase();
    }

    public object? Object1 { get; init; }
    public object? Object2 { get; init; }
    public object? Object3 { get; init; }
}
