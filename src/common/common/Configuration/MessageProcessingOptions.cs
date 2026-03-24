namespace common.Configuration;

public enum MessageProcessingMode
{
    Function,
    Service
}

public sealed class MessageProcessingOptions
{
    public const string SectionName = "MessageProcessing";

    public string Mode { get; init; } = nameof(MessageProcessingMode.Function);

    public MessageProcessingMode ResolveMode()
    {
        if (Enum.TryParse<MessageProcessingMode>(Mode, ignoreCase: true, out var parsedMode))
        {
            return parsedMode;
        }

        throw new InvalidOperationException($"Invalid {SectionName}:Mode value '{Mode}'. Allowed values: Function|Service.");
    }
}