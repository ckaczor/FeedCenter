using Common.Update;
using FeedCenter.Properties;
using System.Windows;

namespace FeedCenter
{
    public partial class MainWindow
    {
        private static void InitializeUpdate()
        {
            UpdateCheck.ApplicationName = Properties.Resources.ApplicationDisplayName;
            UpdateCheck.UpdateServer = Settings.Default.VersionLocation;
            UpdateCheck.UpdateFile = Settings.Default.VersionFile;
            UpdateCheck.ApplicationShutdown = ApplicationShutdown;
            UpdateCheck.ApplicationCurrentMessage = ApplicationCurrentMessage;
            UpdateCheck.ApplicationUpdateMessage = ApplicationUpdateMessage;
        }

        private static bool ApplicationUpdateMessage(string title, string message)
        {
            return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes;
        }

        private static void ApplicationCurrentMessage(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static void ApplicationShutdown()
        {
            Application.Current.Shutdown();
        }

        private void HandleNewVersionLinkClick(object sender, RoutedEventArgs e)
        {
            // Display update information
            UpdateCheck.DisplayUpdateInformation(true);
        }
    }
}
