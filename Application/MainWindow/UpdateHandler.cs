using ChrisKaczor.ApplicationUpdate;
using FeedCenter.Properties;
using System.Windows;

namespace FeedCenter;

public partial class MainWindow
{
    private static void InitializeUpdate()
    {
        UpdateCheck.Initialize(ServerType.GitHub,
            Settings.Default.VersionLocation,
            string.Empty,
            Properties.Resources.ApplicationDisplayName,
            ApplicationShutdown,
            ApplicationCurrentMessage,
            ApplicationUpdateMessage);
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
        UpdateCheck.DisplayUpdateInformation(true);
    }
}