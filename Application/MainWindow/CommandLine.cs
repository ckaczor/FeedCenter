using Common.IO;
using System;

namespace FeedCenter
{
    public partial class MainWindow
    {
        private InterprocessMessageListener _commandLineListener;

        private void HandleCommandLine(object sender, InterprocessMessageListener.InterprocessMessageEventArgs e)
        {
            // If the command line is blank then ignore it
            if (e.Message.Length == 0)
                return;

            // Pad the command line with a trailing space just to be lazy in parsing
            var commandLine = e.Message + " ";

            // Look for the feed URL in the command line
            var startPosition = commandLine.IndexOf("feed://", StringComparison.Ordinal);

            // If we found one then we should extract and process it
            if (startPosition > 0)
            {
                // Advance past the protocol
                startPosition += 7;

                // Starting at the URL position look for the next space
                var endPosition = commandLine.IndexOf(" ", startPosition, StringComparison.Ordinal);

                // Extract the feed URL
                var feedUrl = commandLine.Substring(startPosition, endPosition - startPosition);

                // Add the HTTP protocol by default
                feedUrl = "http://" + feedUrl;

                // Create a new feed using the URL
                HandleNewFeed(feedUrl);
            }
        }
    }
}
