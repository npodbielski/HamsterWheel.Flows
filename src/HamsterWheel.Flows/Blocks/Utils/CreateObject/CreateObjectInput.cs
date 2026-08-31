using HamsterWheel.Utils;

namespace HamsterWheel.Flows.Blocks.Utils;

public class CreateObjectInput
{
    private string _property1 = null!;
    private string? _property2;
    private string? _property3;

    public string Property1
    {
        get => _property1;
        set => _property1 = value.ToPascalCase();
    }

    public string? Property2
    {
        get => _property2;
        set => _property2 = value?.ToPascalCase();
    }

    public string? Property3
    {
        get => _property3;
        set => _property3 = value?.ToPascalCase();
    }

    public object? Object1 { get; init; }
    public object? Object2 { get; init; }
    public object? Object3 { get; init; }
}