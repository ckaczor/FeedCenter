using ChrisKaczor.ApplicationUpdate;

namespace FeedCenter.Options;

public partial class UpdateOptionsPanel
{
    public UpdateOptionsPanel()
    {
        InitializeComponent();
    }

    public override void LoadPanel()
    {
        base.LoadPanel();

        CheckVersionOnStartupCheckBox.IsChecked = Properties.Settings.Default.CheckVersionAtStartup;

        MarkLoaded();
    }

    public override string CategoryName => Properties.Resources.optionCategoryUpdate;

    private void HandleCheckVersionNowButtonClick(object sender, System.Windows.RoutedEventArgs e)
    {
        UpdateCheck.DisplayUpdateInformation(true);
    }

    private void CheckVersionOnStartupCheckBox_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (!HasLoaded) return;

        if (CheckVersionOnStartupCheckBox.IsChecked.HasValue && Properties.Settings.Default.CheckVersionAtStartup != CheckVersionOnStartupCheckBox.IsChecked.Value)
            Properties.Settings.Default.CheckVersionAtStartup = CheckVersionOnStartupCheckBox.IsChecked.Value;
    }
}