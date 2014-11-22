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

            checkVersionOnStartupCheckBox.IsChecked = Properties.Settings.Default.CheckVersionAtStartup;
        }

        public override bool ValidatePanel()
        {
            return true;
        }

        public override void SavePanel()
        {
            if (checkVersionOnStartupCheckBox.IsChecked.HasValue && Properties.Settings.Default.CheckVersionAtStartup != checkVersionOnStartupCheckBox.IsChecked.Value)
                Properties.Settings.Default.CheckVersionAtStartup = checkVersionOnStartupCheckBox.IsChecked.Value;
        }

        public override string CategoryName
        {
            get { return Properties.Resources.optionCategoryUpdate; }
        }

        private void HandleCheckVersionNowButtonClick(object sender, System.Windows.RoutedEventArgs e)
        {
            UpdateCheck.DisplayUpdateInformation(true);
        }
    }
}
