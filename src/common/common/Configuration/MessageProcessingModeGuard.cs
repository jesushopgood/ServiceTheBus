namespace common.Configuration;

public static class MessageProcessingModeGuard
{
    public static void Ensure(MessageProcessingOptions options, MessageProcessingMode expectedMode)
    {
        var actualMode = options.ResolveMode();
        if (actualMode != expectedMode)
        {
            throw new InvalidOperationException(
                $"MessageProcessing mode mismatch. Expected '{expectedMode}' but found '{actualMode}'.");
        }
    }
}