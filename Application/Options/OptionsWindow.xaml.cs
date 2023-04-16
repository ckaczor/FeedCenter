using System.Collections.Generic;
using System.Windows.Controls;

namespace FeedCenter.Options;

public partial class OptionsWindow
{
    private readonly List<OptionsPanelBase> _optionPanels = new();

    public OptionsWindow()
    {
        InitializeComponent();

        // Add all the option categories
        AddCategories();

        // Load the category list
        LoadCategories();
    }

    private void AddCategories()
    {
        _optionPanels.Add(new GeneralOptionsPanel(this));
        _optionPanels.Add(new DisplayOptionsPanel(this));
        _optionPanels.Add(new FeedsOptionsPanel(this));
        _optionPanels.Add(new UpdateOptionsPanel(this));
        _optionPanels.Add(new AboutOptionsPanel(this));
    }

    private void LoadCategories()
    {
        // Loop over each panel
        foreach (var optionsPanel in _optionPanels)
        {
            // Tell the panel to load itself
            optionsPanel.LoadPanel();

            // Add the panel to the category ist
            CategoryListBox.Items.Add(new CategoryListItem(optionsPanel));

            // Set the panel into the right side
            ContentControl.Content = optionsPanel;
        }

        // Select the first item
        CategoryListBox.SelectedItem = CategoryListBox.Items[0];
    }

    private void SelectCategory(OptionsPanelBase panel)
    {
        // Set the content
        ContentControl.Content = panel;
    }

    private void HandleSelectedCategoryChanged(object sender, SelectionChangedEventArgs e)
    {
        // Select the right category
        SelectCategory(((CategoryListItem) CategoryListBox.SelectedItem).Panel);
    }

    private class CategoryListItem
    {
        public CategoryListItem(OptionsPanelBase panel)
        {
            Panel = panel;
        }

        public OptionsPanelBase Panel { get; }

        public override string ToString()
        {
            return Panel.CategoryName;
        }
    }
}