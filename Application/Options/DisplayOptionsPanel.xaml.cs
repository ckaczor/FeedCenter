using FeedCenter.Properties;
using System.Linq;
using System.Windows.Controls;

namespace FeedCenter.Options
{
    public partial class DisplayOptionsPanel
    {
        public DisplayOptionsPanel()
        {
            InitializeComponent();
        }

        public override void LoadPanel(FeedCenterEntities database)
        {
            base.LoadPanel(database);

            LockWindowCheckBox.IsChecked = Settings.Default.WindowLocked;
            DisplayEmptyFeedsCheckBox.IsChecked = Settings.Default.DisplayEmptyFeeds;
            ToolbarLocationComboBox.SelectedItem = ToolbarLocationComboBox.Items.Cast<ComboBoxItem>().First(comboBoxItem => (Dock) comboBoxItem.Tag == Settings.Default.ToolbarLocation);
            MultipleLineDisplayComboBox.SelectedItem = MultipleLineDisplayComboBox.Items.Cast<ComboBoxItem>().First(comboBoxItem => (MultipleLineDisplay) comboBoxItem.Tag == Settings.Default.MultipleLineDisplay);
        }

        public override bool ValidatePanel()
        {
            return true;
        }

        public override void SavePanel()
        {
            if (LockWindowCheckBox.IsChecked.HasValue && Settings.Default.WindowLocked != LockWindowCheckBox.IsChecked.Value)
                Settings.Default.WindowLocked = LockWindowCheckBox.IsChecked.Value;

            if (DisplayEmptyFeedsCheckBox.IsChecked.HasValue && Settings.Default.DisplayEmptyFeeds != DisplayEmptyFeedsCheckBox.IsChecked.Value)
                Settings.Default.DisplayEmptyFeeds = DisplayEmptyFeedsCheckBox.IsChecked.Value;

            var dock = (Dock) ((ComboBoxItem) ToolbarLocationComboBox.SelectedItem).Tag;
            Settings.Default.ToolbarLocation = dock;

            var multipleLineDisplay = (MultipleLineDisplay) ((ComboBoxItem) MultipleLineDisplayComboBox.SelectedItem).Tag;
            Settings.Default.MultipleLineDisplay = multipleLineDisplay;
        }

        public override string CategoryName => Properties.Resources.optionCategoryDisplay;
    }
}
