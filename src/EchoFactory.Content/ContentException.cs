namespace EchoFactory.Content;

/// <summary>
/// Raised when content (node / level / solution JSON) fails to load or validate.
/// Messages are human-readable and name the source file and what is wrong ("fail loud").
/// </summary>
public sealed class ContentException : Exception
{
    public ContentException(string message)
        : base(message)
    {
    }

    public ContentException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
