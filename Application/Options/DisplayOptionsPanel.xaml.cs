using FeedCenter.Properties;
using System.Linq;
using System.Windows.Controls;

namespace FeedCenter.Options;

public partial class DisplayOptionsPanel
{
    public DisplayOptionsPanel()
    {
        InitializeComponent();
    }

    public override void LoadPanel()
    {
        base.LoadPanel();

        LockWindowCheckBox.IsChecked = Settings.Default.WindowLocked;
        DisplayEmptyFeedsCheckBox.IsChecked = Settings.Default.DisplayEmptyFeeds;
        ToolbarLocationComboBox.SelectedItem = ToolbarLocationComboBox.Items.Cast<ComboBoxItem>().First(comboBoxItem => (Dock) comboBoxItem.Tag == Settings.Default.ToolbarLocation);
        MultipleLineDisplayComboBox.SelectedItem = MultipleLineDisplayComboBox.Items.Cast<ComboBoxItem>().First(comboBoxItem => (MultipleLineDisplay) comboBoxItem.Tag == Settings.Default.MultipleLineDisplay);

        MarkLoaded();
    }

    public override string CategoryName => Properties.Resources.optionCategoryDisplay;

    private void LockWindowCheckBox_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (!HasLoaded) return;

        if (LockWindowCheckBox.IsChecked.HasValue && Settings.Default.WindowLocked != LockWindowCheckBox.IsChecked.Value)
            Settings.Default.WindowLocked = LockWindowCheckBox.IsChecked.Value;
    }

    private void DisplayEmptyFeedsCheckBox_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (!HasLoaded) return;

        if (DisplayEmptyFeedsCheckBox.IsChecked.HasValue && Settings.Default.DisplayEmptyFeeds != DisplayEmptyFeedsCheckBox.IsChecked.Value)
            Settings.Default.DisplayEmptyFeeds = DisplayEmptyFeedsCheckBox.IsChecked.Value;
    }

    private void ToolbarLocationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!HasLoaded) return;

        var dock = (Dock) ((ComboBoxItem) ToolbarLocationComboBox.SelectedItem).Tag;
        Settings.Default.ToolbarLocation = dock;
    }

    private void MultipleLineDisplayComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!HasLoaded) return;

        var multipleLineDisplay = (MultipleLineDisplay) ((ComboBoxItem) MultipleLineDisplayComboBox.SelectedItem).Tag;
        Settings.Default.MultipleLineDisplay = multipleLineDisplay;
    }
}