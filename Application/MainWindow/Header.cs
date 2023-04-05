using CKaczor.InstalledBrowsers;
using FeedCenter.Properties;
using System.Windows;
using System.Windows.Input;

namespace FeedCenter
{
    public partial class MainWindow
    {
        private void HandleHeaderLabelMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Ignore if the window is locked
            if (Settings.Default.WindowLocked)
                return;

            // Start dragging
            DragMove();
        }

        private void HandleCloseButtonClick(object sender, RoutedEventArgs e)
        {
            // Close the window
            Close();
        }

        private void HandleFeedLabelMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Open the link for the current feed on a left double click
            if (e.ClickCount == 2 && e.ChangedButton == MouseButton.Left)
                InstalledBrowser.OpenLink(Settings.Default.Browser, _currentFeed.Link);
        }
    }
}