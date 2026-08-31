namespace HamsterWheel.Flows.Tests;

public class SinglePropInput
{
    public string Prop { get; set; } = null!;
}

public class SinglePropInput<T>
{
    public T Prop { get; set; } = default!;
}

public class TwoPropsInput
{
    public string Prop { get; set; } = null!;
    public int Prop2 { get; set; }
}

public class ThreePropsInput
{
    public string Prop { get; set; } = null!;
    public int Prop2 { get; set; }
    public TestEnum Prop3 { get; set; }
}

public class FourPropsInput
{
    public string Prop { get; set; } = null!;
    public int Prop2 { get; set; }
    public TestEnum Prop3 { get; set; }
    public DateTime Prop4 { get; set; }
}

public class FivePropsInput
{
    public string Prop { get; set; } = null!;
    public int Prop2 { get; set; }
    public TestEnum Prop3 { get; set; }
    public DateTime Prop4 { get; set; }
    public byte Prop5 { get; set; }
}

public class SixPropsInput
{
    public string Prop { get; set; } = null!;
    public int Prop2 { get; set; }
    public TestEnum Prop3 { get; set; }
    public DateTime Prop4 { get; set; }
    public byte Prop5 { get; set; }
    public uint Prop6 { get; set; }
}

public class ClassRequiredProp
{
    public required string Prop { get; set; } = null!;
    public int PropInt { get; private set; }
}

public enum TestEnum
{
    YYY = 1,
    Other = 10
}