using Common.Debug;
using Common.Internet;
using FeedCenter.Properties;
using System;
using System.Diagnostics;

namespace FeedCenter
{
    public static class BrowserCommon
    {
        public static Browser FindBrowser(string browserKey)
        {
            Browser browser = null;

            // Get the list of installed browsers
            var browsers = Browser.DetectInstalledBrowsers();

            // Make sure the desired browser exists
            if (browsers.ContainsKey(browserKey))
            {
                // Get the browser
                browser = browsers[browserKey];
            }

            return browser;
        }

        public static bool OpenLink(string url)
        {
            // Get the browser 
            Browser browser = FindBrowser(Settings.Default.Browser);

            // Start the browser
            return OpenLink(browser, url);
        }

        public static bool OpenLink(Browser browser, string url)
        {
            try
            {
                // Don't bother with empty links
                if (String.IsNullOrEmpty(url))
                    return true;

                // Add quotes around the URL for safety
                url = string.Format("\"{0}\"", url);

                // Start the browser
                if (browser == null)
                    Process.Start(url);
                else
                    Process.Start(browser.Command, url);

                return true;
            }
            catch (Exception exception)
            {
                // Just log the exception
                Tracer.WriteException(exception);

                return false;
            }
        }
    }
}
