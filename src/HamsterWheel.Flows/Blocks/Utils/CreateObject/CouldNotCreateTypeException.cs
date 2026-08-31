namespace HamsterWheel.Flows.Blocks.Utils;

public class CouldNotCreateTypeException(string blockId, Type type)
    : BlockException(
        $"Block: '{blockId}' of type: '{nameof(CreateObjectBlock)}' could not create instance of type: '{type.Name}'");