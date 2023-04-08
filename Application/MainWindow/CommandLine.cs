using System;

namespace FeedCenter
{
    public partial class MainWindow
    {
        private void HandleCommandLine(string commandLine)
        {
            // If the command line is blank then ignore it
            if (commandLine.Length == 0)
                return;

            // Pad the command line with a trailing space just to be lazy in parsing
            commandLine += " ";

            // Look for the feed URL in the command line
            var startPosition = commandLine.IndexOf("feed://", StringComparison.Ordinal);

            // If nothing was found then exit
            if (startPosition <= 0) return;

            // Advance past the protocol
            startPosition += 7;

            // Starting at the URL position look for the next space
            var endPosition = commandLine.IndexOf(" ", startPosition, StringComparison.Ordinal);

            // Extract the feed URL
            var feedUrl = commandLine[startPosition..endPosition];

            // Add the HTTP protocol by default
            feedUrl = "http://" + feedUrl;

            // Create a new feed using the URL
            HandleNewFeed(feedUrl);
        }
    }
}