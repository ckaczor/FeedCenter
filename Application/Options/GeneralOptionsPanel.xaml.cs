using ChrisKaczor.Wpf.Application;
using FeedCenter.Properties;
using System.Windows;

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

        Application.Current.SetStartWithWindows(Settings.Default.StartWithWindows);
    }
}