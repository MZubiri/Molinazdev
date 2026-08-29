namespace DigitalServices.Application.Common;

public sealed class ResourceNotFoundException : Exception
{
    public ResourceNotFoundException(string resourceName, object resourceKey)
        : base($"{resourceName} '{resourceKey}' was not found.")
    {
        ResourceName = resourceName;
        ResourceKey = resourceKey;
    }

    public string ResourceName { get; }

    public object ResourceKey { get; }
}
