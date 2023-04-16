using System;
using System.Linq;
using System.Net;
using System.Windows;

namespace FeedCenter;

public partial class MainWindow
{
    private readonly string[] _chromeExtensions = { "chrome-extension://ehojfdcmnajoklleckniaifaijfnkpbi/subscribe.html?", "chrome-extension://nlbjncdgjeocebhnmkbbbdekmmmcbfjd/subscribe.html?" };

    private void HandleDragOver(object sender, DragEventArgs e)
    {
        // Default to not allowed
        e.Effects = DragDropEffects.None;
        e.Handled = true;

        // If there isn't any text in the data then it is not allowed
        if (!e.Data.GetDataPresent(DataFormats.Text))
            return;

        // Get the data as a string
        var data = (string) e.Data.GetData(DataFormats.Text);

        // If the data doesn't look like a URI then it is not allowed
        if (!Uri.IsWellFormedUriString(data, UriKind.Absolute))
            return;

        // Allowed
        e.Effects = DragDropEffects.Copy;
    }

    private void HandleDragDrop(object sender, DragEventArgs e)
    {
        // Get the data as a string
        var data = (string) e.Data.GetData(DataFormats.Text);

        if (string.IsNullOrEmpty(data))
            return;

        // Check to see if the data starts with any known Chrome extension
        var chromeExtension = _chromeExtensions.FirstOrDefault(data.StartsWith);

        // Remove the Chrome extension URL and decode the URL
        if (chromeExtension != null)
        {
            data = data[chromeExtension.Length..];
            data = WebUtility.UrlDecode(data);
        }

        // Handle the new feed but allow the drag/drop to complete
        Dispatcher.BeginInvoke(new NewFeedDelegate(HandleNewFeed), data);
    }
}