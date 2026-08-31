namespace HamsterWheel.Flows.IO;

//The base GlobalInputTransformer is abstract on purpose - hosts subclass it to register block-specific input transformations.
//This default keeps the DI chain resolvable when the host does not provide one
public sealed class DefaultGlobalInputTransformer : GlobalInputTransformer;
