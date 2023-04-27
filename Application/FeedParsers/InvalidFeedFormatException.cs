using System;

namespace FeedCenter.FeedParsers;

[Serializable]
internal class InvalidFeedFormatException : ApplicationException
{
    internal InvalidFeedFormatException(Exception exception)
        : base(string.Empty, exception)
    {
    }
}