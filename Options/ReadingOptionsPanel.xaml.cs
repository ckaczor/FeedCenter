using System.Windows.Controls;

using Common.Internet;
using Common.Wpf.Extensions;

namespace FeedCenter.Options
{
    public partial class ReadingOptionsPanel
    {
        public ReadingOptionsPanel()
        {
            InitializeComponent();
        }

        public override void LoadPanel(FeedCenterEntities database)
        {
            base.LoadPanel(database);

            var settings = Properties.Settings.Default;

            LoadBrowserComboBox(browserComboBox, settings.Browser);
        }

        public override bool ValidatePanel()
        {
            return true;
        }

        public override void SavePanel()
        {
            var settings = Properties.Settings.Default;

            string browser = (string) ((ComboBoxItem) browserComboBox.SelectedItem).Tag;
            if (settings.Browser != browser)
                settings.Browser = browser;

            this.UpdateAllSources();
        }

        public override string CategoryName
        {
            get { return Properties.Resources.optionCategoryReading; }
        }

        private static void LoadBrowserComboBox(ComboBox comboBox, string selected)
        {
            comboBox.SelectedIndex = 0;

            ComboBoxItem selectedItem = null;

            var browsers = Browser.DetectInstalledBrowsers();
            foreach (var browser in browsers)
            {
                ComboBoxItem item = new ComboBoxItem { Content = browser.Value.Name, Tag = browser.Key };

                comboBox.Items.Add(item);

                if (browser.Key == selected)
                    selectedItem = item;
            }

            if (selectedItem != null)
                comboBox.SelectedItem = selectedItem;
        }
    }
}
