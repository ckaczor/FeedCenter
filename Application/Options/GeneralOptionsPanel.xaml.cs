namespace FeedCenter.Options
{
    public partial class GeneralOptionsPanel
    {
        public GeneralOptionsPanel()
        {
            InitializeComponent();
        }

        public override void LoadPanel(FeedCenterEntities database)
        {
            base.LoadPanel(database);

            var settings = Properties.Settings.Default;

            startWithWindowsCheckBox.IsChecked = settings.StartWithWindows;
            registerAsDefaultFeedReaderCheckBox.IsChecked = settings.RegisterAsDefaultFeedReader;
        }

        public override bool ValidatePanel()
        {
            return true;
        }

        public override void SavePanel()
        {
            var settings = Properties.Settings.Default;

            if (startWithWindowsCheckBox.IsChecked.HasValue && settings.StartWithWindows != startWithWindowsCheckBox.IsChecked.Value)
                settings.StartWithWindows = startWithWindowsCheckBox.IsChecked.Value;

            if (registerAsDefaultFeedReaderCheckBox.IsChecked.HasValue && settings.RegisterAsDefaultFeedReader != registerAsDefaultFeedReaderCheckBox.IsChecked.Value)
                settings.RegisterAsDefaultFeedReader = registerAsDefaultFeedReaderCheckBox.IsChecked.Value;

            App.SetStartWithWindows(settings.StartWithWindows);
            App.SetDefaultFeedReader(settings.RegisterAsDefaultFeedReader);
        }

        public override string CategoryName
        {
            get { return Properties.Resources.optionCategoryGeneral; }
        }
    }
}
