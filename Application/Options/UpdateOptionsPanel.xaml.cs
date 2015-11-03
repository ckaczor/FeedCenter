using Common.Update;

namespace FeedCenter.Options
{
    public partial class UpdateOptionsPanel
    {
        public UpdateOptionsPanel()
        {
            InitializeComponent();
        }

        public override void LoadPanel(FeedCenterEntities database)
        {
            base.LoadPanel(database);

            CheckVersionOnStartupCheckBox.IsChecked = Properties.Settings.Default.CheckVersionAtStartup;
        }

        public override bool ValidatePanel()
        {
            return true;
        }

        public override void SavePanel()
        {
            if (CheckVersionOnStartupCheckBox.IsChecked.HasValue && Properties.Settings.Default.CheckVersionAtStartup != CheckVersionOnStartupCheckBox.IsChecked.Value)
                Properties.Settings.Default.CheckVersionAtStartup = CheckVersionOnStartupCheckBox.IsChecked.Value;
        }

        public override string CategoryName => Properties.Resources.optionCategoryUpdate;

        private void HandleCheckVersionNowButtonClick(object sender, System.Windows.RoutedEventArgs e)
        {
            UpdateCheck.DisplayUpdateInformation(true);
        }
    }
}
