namespace FeedCenter;

public enum FeedReadResult
{
    Success,
    NotModified,
    NotDue,
    UnknownError,
    InvalidXml,
    NotEnabled,
    Unauthorized,
    NoResponse,
    NotFound,
    Timeout,
    ConnectionFailed,
    ServerError,
    Moved,
    TemporarilyUnavailable
}