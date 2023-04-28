using System;
using FeedCenter.Properties;
using H.NotifyIcon;
using System.Windows.Controls;

namespace FeedCenter;

internal static class NotificationIcon
{
    private static MainWindow _mainWindow;
    private static TaskbarIcon _notificationIcon;
    private static MenuItem _lockMenuItem;

    public static void Initialize(MainWindow mainWindow)
    {
        // Store the main window
        _mainWindow = mainWindow;

        // Create the notification icon
        _notificationIcon = new TaskbarIcon { Icon = Resources.Application, Id = Guid.Parse("2F4CE5CA-BC20-4D0D-80D0-45F13C6F4905") };
        _notificationIcon.TrayMouseDoubleClick += HandleNotificationIconDoubleClick;

        // Setup the menu
        var contextMenu = new ContextMenu();
        contextMenu.Opened += HandleContextMenuOpened;

        _lockMenuItem = new MenuItem()
        {
            Header = Resources.NotificationIconContextMenuLocked,
            IsChecked = Settings.Default.WindowLocked
        };
        _lockMenuItem.Click += HandleLockWindowClicked;
        contextMenu.Items.Add(_lockMenuItem);

        contextMenu.Items.Add(new Separator());

        var menuItem = new MenuItem()
        {
            Header = Resources.NotificationIconContextMenuExit
        };
        menuItem.Click += HandleContextMenuExitClick;
        contextMenu.Items.Add(menuItem);

        // Set the menu into the icon
        _notificationIcon.ContextMenu = contextMenu;

        // Show the icon
        _notificationIcon.ForceCreate(false);
    }

    private static void HandleContextMenuOpened(object sender, System.Windows.RoutedEventArgs e)
    {
        _lockMenuItem.IsChecked = Settings.Default.WindowLocked;
    }

    private static void HandleNotificationIconDoubleClick(object sender, System.EventArgs e)
    {
        // Bring the main form to the front
        _mainWindow.Activate();
    }

    private static void HandleContextMenuExitClick(object sender, System.EventArgs e)
    {
        // Close the main form
        _mainWindow.Close();
    }

    private static void HandleLockWindowClicked(object sender, System.EventArgs e)
    {
        // Toggle the lock setting
        Settings.Default.WindowLocked = !Settings.Default.WindowLocked;

        // Refresh the menu choice
        _lockMenuItem.IsChecked = Settings.Default.WindowLocked;
    }

    public static void Dispose()
    {
        // Get rid of the icon
        _notificationIcon.Dispose();
        _notificationIcon = null;

        _mainWindow = null;
    }

    public static void ShowBalloonTip(string text, H.NotifyIcon.Core.NotificationIcon icon)
    {
        _notificationIcon.ShowNotification(Resources.ApplicationDisplayName, text, icon);
    }
}