using ChrisKaczor.InstalledBrowsers;
using ChrisKaczor.Wpf.Application;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace FeedCenter.Options;

public partial class GeneralOptionsPanel
{
    public GeneralOptionsPanel(Window parentWindow) : base(parentWindow)
    {
        InitializeComponent();
    }

    public override string CategoryName => Properties.Resources.optionCategoryGeneral;

    public override void LoadPanel()
    {
        base.LoadPanel();

        var settings = Properties.Settings.Default;

        StartWithWindowsCheckBox.IsChecked = settings.StartWithWindows;

        LoadBrowserComboBox(BrowserComboBox, settings.Browser);

        LoadUserAgentComboBox(UserAgentComboBox, settings.DefaultUserAgent);

        MarkLoaded();
    }

    private static void LoadBrowserComboBox(Selector selector, string selected)
    {
        selector.SelectedIndex = 0;

        ComboBoxItem selectedItem = null;

        var browsers = InstalledBrowser.GetInstalledBrowsers(true);
        foreach (var browser in browsers)
        {
            var item = new ComboBoxItem { Content = browser.Value.Name, Tag = browser.Key };

            selector.Items.Add(item);

            if (browser.Key == selected)
                selectedItem = item;
        }

        if (selectedItem != null)
            selector.SelectedItem = selectedItem;
    }

    private static void LoadUserAgentComboBox(Selector selector, string selected)
    {
        selector.SelectedIndex = 0;

        ComboBoxItem selectedItem = null;

        var userAgents = UserAgentItem.GetUserAgents();
        foreach (var userAgent in userAgents)
        {
            var item = new ComboBoxItem { Content = userAgent.Caption, Tag = userAgent.UserAgent };

            selector.Items.Add(item);

            if (userAgent.UserAgent == selected)
                selectedItem = item;
        }

        if (selectedItem != null)
            selector.SelectedItem = selectedItem;
    }

    private void StartWithWindowsCheckBox_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (!HasLoaded) return;

        var settings = Properties.Settings.Default;

        if (StartWithWindowsCheckBox.IsChecked.HasValue &&
            settings.StartWithWindows != StartWithWindowsCheckBox.IsChecked.Value)
            settings.StartWithWindows = StartWithWindowsCheckBox.IsChecked.Value;

        System.Windows.Application.Current.SetStartWithWindows(settings.StartWithWindows);
    }

    private void BrowserComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!HasLoaded) return;

        var settings = Properties.Settings.Default;

        settings.Browser = (string) ((ComboBoxItem) BrowserComboBox.SelectedItem).Tag;
    }

    private void UserAgentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!HasLoaded) return;

        var settings = Properties.Settings.Default;

        settings.DefaultUserAgent = (string) ((ComboBoxItem) UserAgentComboBox.SelectedItem).Tag;
    }
}