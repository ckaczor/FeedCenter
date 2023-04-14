using FeedCenter.Properties;
using System.Windows;

namespace FeedCenter.Options;

public partial class DisplayOptionsPanel
{
    public DisplayOptionsPanel(Window parentWindow) : base(parentWindow)
    {
        InitializeComponent();
    }

    public override string CategoryName => Properties.Resources.optionCategoryDisplay;

    public override void LoadPanel()
    {
        base.LoadPanel();

        MarkLoaded();
    }

    private void OnSaveSettings(object sender, RoutedEventArgs e)
    {
        SaveSettings();
    }

    private void SaveSettings()
    {
        if (!HasLoaded) return;

        Settings.Default.Save();
    }
}