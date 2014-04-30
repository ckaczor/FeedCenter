using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

using Common.Wpf.Extensions;

namespace FeedCenter.Options
{
    public partial class FeedWindow
    {
        public FeedWindow()
        {
            InitializeComponent();
        }

        public bool? Display(FeedCenterEntities database, Feed feed, Window owner)
        {
            // Bind the category combo box
            categoryComboBox.ItemsSource = database.Categories;

            // Set the data context
            DataContext = feed;

            // Set the title based on the state of the feed
            Title = string.IsNullOrWhiteSpace(feed.Link) ? Properties.Resources.FeedWindowAdd : Properties.Resources.FeedWindowEdit;

            // Set the window owner
            Owner = owner;

            // Show the dialog and result the result
            return ShowDialog();
        }

        private void HandleOkayButtonClick(object sender, RoutedEventArgs e)
        {
            // Get a dictionary of all framework elements and explicit binding expressions
            Dictionary<FrameworkElement, BindingExpression> bindingExpressionDictionary = this.GetExplicitBindingExpressions();

            // Get just the binding expressions
            var bindingExpressions = bindingExpressionDictionary.Values;

            // Loop over each binding expression and clear any existing error
            this.ClearAllValidationErrors(bindingExpressions);

            // Force all explicit bindings to update the source
            this.UpdateAllSources(bindingExpressions);

            // See if there are any errors
            bool hasError = bindingExpressions.Any(b => b.HasError);

            // If there was an error then set focus to the bad controls
            if (hasError)
            {
                // Get the first framework element with an error
                FrameworkElement firstErrorElement = bindingExpressionDictionary.First(pair => pair.Value.HasError).Key;

                // Loop over each tab item
                foreach (TabItem tabItem in optionsTabControl.Items)
                {
                    // Cast the content as visual
                    Visual content = (Visual) tabItem.Content;

                    // See if the control with the error is a descendant 
                    if (firstErrorElement.IsDescendantOf(content))
                    {
                        // Select the tab
                        tabItem.IsSelected = true;
                        break;
                    }
                }

                // Set focus
                firstErrorElement.Focus();

                return;
            }

            // Dialog is good
            DialogResult = true;

            // Close the dialog
            Close();
        }
    }
}
