using System;

namespace FeedCenter.FeedParsers
{
    internal class FeedParseException : ApplicationException
    {
        public FeedParseException(FeedParseError feedParseError)
        {
            ParseError = feedParseError;
        }

        public FeedParseError ParseError { get; set; }
    }
}