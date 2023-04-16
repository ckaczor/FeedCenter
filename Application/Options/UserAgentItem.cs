using System.Collections.Generic;

namespace FeedCenter.Options;

public class UserAgentItem
{
    public string Caption { get; set; }
    public string UserAgent { get; set; }

    public static List<UserAgentItem> UserAgents => new()
    {
        new UserAgentItem
        {
            Caption = Properties.Resources.DefaultUserAgentCaption,
            UserAgent = string.Empty
        },
        new UserAgentItem
        {
            Caption = "Windows RSS Platform 2.0",
            UserAgent = "Windows-RSS-Platform/2.0 (MSIE 9.0; Windows NT 6.1)"
        },
        new UserAgentItem
        {
            Caption = "Feedly 1.0",
            UserAgent = "Feedly/1.0"
        },
        new UserAgentItem
        {
            Caption = "curl",
            UserAgent = "curl/7.47.0"
        }
    };
}