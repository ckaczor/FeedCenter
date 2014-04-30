using System.Linq;
using System.Windows.Controls;

using FeedCenter.Properties;

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

            lockWindowCheckBox.IsChecked = Settings.Default.WindowLocked;
            displayEmptyFeedsCheckBox.IsChecked = Settings.Default.DisplayEmptyFeeds;
            toolbarLocationComboBox.SelectedItem = toolbarLocationComboBox.Items.Cast<ComboBoxItem>().First(comboBoxItem => (Dock) comboBoxItem.Tag == Settings.Default.ToolbarLocation);
            multipleLineDisplayComboBox.SelectedItem = multipleLineDisplayComboBox.Items.Cast<ComboBoxItem>().First(comboBoxItem => (MultipleLineDisplay) comboBoxItem.Tag == Settings.Default.MultipleLineDisplay);
        }

        public override bool ValidatePanel()
        {
            return true;
        }

        public override void SavePanel()
        {
            if (lockWindowCheckBox.IsChecked.HasValue && Settings.Default.WindowLocked != lockWindowCheckBox.IsChecked.Value)
                Settings.Default.WindowLocked = lockWindowCheckBox.IsChecked.Value;

            if (displayEmptyFeedsCheckBox.IsChecked.HasValue && Settings.Default.DisplayEmptyFeeds != displayEmptyFeedsCheckBox.IsChecked.Value)
                Settings.Default.DisplayEmptyFeeds = displayEmptyFeedsCheckBox.IsChecked.Value;

            Dock dock = (Dock) ((ComboBoxItem) toolbarLocationComboBox.SelectedItem).Tag;
            if (Settings.Default.ToolbarLocation != dock)
                Settings.Default.ToolbarLocation = dock;

            MultipleLineDisplay multipleLineDisplay = (MultipleLineDisplay) ((ComboBoxItem) multipleLineDisplayComboBox.SelectedItem).Tag;
            if (Settings.Default.MultipleLineDisplay != multipleLineDisplay)
                Settings.Default.MultipleLineDisplay = multipleLineDisplay;
        }

        public override string CategoryName
        {
            get { return Properties.Resources.optionCategoryDisplay; }
        }
    }
}
