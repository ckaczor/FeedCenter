using System.Collections.Generic;

namespace FeedCenter.Options
{
    internal class UserAgentItem
    {
        internal string Caption { get; set; }
        internal string UserAgent { get; set; }

        internal static List<UserAgentItem> GetUserAgents()
        {
            var userAgents = new List<UserAgentItem>
            {
                new()
                {
                    Caption = Properties.Resources.DefaultUserAgentCaption,
                    UserAgent = string.Empty
                },
                new()
                {
                    Caption = "Windows RSS Platform 2.0",
                    UserAgent = "Windows-RSS-Platform/2.0 (MSIE 9.0; Windows NT 6.1)"
                },
                new()
                {
                    Caption = "Feedly 1.0",
                    UserAgent = "Feedly/1.0"
                },
                new()
                {
                    Caption = "curl",
                    UserAgent = "curl/7.47.0"
                }
            };

            return userAgents;
        }
    }
}