using Common.Internet;
using Common.Wpf.Extensions;
using System.Windows.Controls;
using System.Windows.Data;

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

            var browser = (string) ((ComboBoxItem) browserComboBox.SelectedItem).Tag;

            settings.Browser = browser;

            var expressions = this.GetBindingExpressions(new[] { UpdateSourceTrigger.Explicit });
            this.UpdateAllSources(expressions);
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
                var item = new ComboBoxItem { Content = browser.Value.Name, Tag = browser.Key };

                comboBox.Items.Add(item);

                if (browser.Key == selected)
                    selectedItem = item;
            }

            if (selectedItem != null)
                comboBox.SelectedItem = selectedItem;
        }
    }
}
