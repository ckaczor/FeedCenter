using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using FeedCenter.Data;

namespace FeedCenter.Options
{
    public partial class OptionsWindow
    {
        #region Member variables

        private readonly List<OptionsPanelBase> _optionPanels = new List<OptionsPanelBase>();

        private readonly FeedCenterEntities _database = Database.Entities;

        #endregion

        #region Constructor

        public OptionsWindow()
        {
            InitializeComponent();

            // Add all the option categories
            AddCategories();

            // Load the category list
            LoadCategories();
        }

        #endregion

        #region Category handling

        private void AddCategories()
        {
            _optionPanels.Add(new GeneralOptionsPanel());
            _optionPanels.Add(new DisplayOptionsPanel());
            _optionPanels.Add(new FeedsOptionsPanel());
            _optionPanels.Add(new UpdateOptionsPanel());
            _optionPanels.Add(new AboutOptionsPanel());
        }

        private void LoadCategories()
        {
            // Loop over each panel
            foreach (var optionsPanel in _optionPanels)
            {
                // Tell the panel to load itself
                optionsPanel.LoadPanel(_database);

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

        #endregion

        #region Category list item

        private class CategoryListItem
        {
            public OptionsPanelBase Panel { get; }

            public CategoryListItem(OptionsPanelBase panel)
            {
                Panel = panel;
            }

            public override string ToString()
            {
                return Panel.CategoryName;
            }
        }

        #endregion

        private void HandleOkayButtonClick(object sender, RoutedEventArgs e)
        {
            // Loop over each panel and ask them to validate
            foreach (var optionsPanel in _optionPanels)
            {
                // If validation fails...
                if (!optionsPanel.ValidatePanel())
                {
                    // ...select the right category
                    SelectCategory(optionsPanel);

                    // Stop validation
                    return;
                }
            }

            // Loop over each panel and ask them to save
            foreach (var optionsPanel in _optionPanels)
            {
                // Save!
                optionsPanel.SavePanel();
            }

            // Save the actual settings
            _database.SaveChanges(() => { });
            Properties.Settings.Default.Save();

            DialogResult = true;

            // Close the window
            Close();
        }
    }
}
