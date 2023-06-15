using System.Collections.Generic;
using System.Linq;
using FeedCenter.Properties;

namespace FeedCenter.Options;

public class UserAgentItem
{
    public string Caption { get; set; }

    public static List<UserAgentItem> DefaultUserAgents => new()
    {
        new UserAgentItem
        {
            Caption = Properties.Resources.ApplicationUserAgentCaption,
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

    public string UserAgent { get; set; }

    public static List<UserAgentItem> UserAgents
    {
        get
        {
            var defaultUserAgents = DefaultUserAgents;

            var applicationDefaultUserAgent = defaultUserAgents.First(dua => dua.UserAgent == Settings.Default.DefaultUserAgent);

            var userAgents = new List<UserAgentItem>
            {
                new()
                {
                    Caption = string.Format(Resources.DefaultUserAgentCaption, applicationDefaultUserAgent.Caption),
                    UserAgent = null
                }
            };

            userAgents.AddRange(defaultUserAgents);

            return userAgents;
        }
    }
}