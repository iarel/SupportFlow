namespace SupportFlow.BuildingBlocks.Application;

/// <summary>
/// The caller may not access the resource, for example another customer's conversation (components.md §1.3).
/// Maps to 403.
/// </summary>
public class AccessDeniedException : Exception
{
    public AccessDeniedException()
    {
    }

    public AccessDeniedException(string message)
        : base(message)
    {
    }

    public AccessDeniedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
