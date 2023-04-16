using FeedCenter.Properties;
using System.Windows.Forms;

namespace FeedCenter;

internal static class NotificationIcon
{
    private static MainWindow _mainForm;
    private static NotifyIcon _notificationIcon;

    public static void Initialize(MainWindow mainForm)
    {
        // Store the main window
        _mainForm = mainForm;

        // Create the notification icon
        _notificationIcon = new NotifyIcon { Icon = Resources.Application };
        _notificationIcon.DoubleClick += HandleNotificationIconDoubleClick;

        // Setup the menu
        var contextMenuStrip = new ContextMenuStrip();

        var toolStripMenuItem = new ToolStripMenuItem(Resources.NotificationIconContextMenuLocked, null, HandleLockWindowClicked) { Checked = Settings.Default.WindowLocked };
        contextMenuStrip.Items.Add(toolStripMenuItem);

        contextMenuStrip.Items.Add(new ToolStripSeparator());

        contextMenuStrip.Items.Add(Resources.NotificationIconContextMenuExit, null, HandleContextMenuExitClick);

        // Set the menu into the icon
        _notificationIcon.ContextMenuStrip = contextMenuStrip;

        // Show the icon
        _notificationIcon.Visible = true;
    }

    private static void HandleNotificationIconDoubleClick(object sender, System.EventArgs e)
    {
        // Bring the main form to the front
        _mainForm.Activate();
    }

    private static void HandleContextMenuExitClick(object sender, System.EventArgs e)
    {
        // Close the main form
        _mainForm.Close();
    }

    private static void HandleLockWindowClicked(object sender, System.EventArgs e)
    {
        // Toggle the lock setting
        Settings.Default.WindowLocked = !Settings.Default.WindowLocked;

        // Refresh the menu choice
        ((ToolStripMenuItem) sender).Checked = Settings.Default.WindowLocked;
    }

    public static void Dispose()
    {
        // Get rid of the icon
        _notificationIcon.Visible = false;
        _notificationIcon.Dispose();
        _notificationIcon = null;

        _mainForm = null;
    }

    public static void ShowBalloonTip(string text, ToolTipIcon icon)
    {
        ShowBalloonTip(text, icon, Settings.Default.BalloonTipTimeout);
    }

    private static void ShowBalloonTip(string text, ToolTipIcon icon, int timeout)
    {
        _notificationIcon.ShowBalloonTip(timeout, Resources.ApplicationDisplayName, text, icon);
    }
}