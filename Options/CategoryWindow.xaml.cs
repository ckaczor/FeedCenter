using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using Common.Wpf.Extensions;

namespace FeedCenter.Options
{
    public partial class CategoryWindow
    {
        public CategoryWindow()
        {
            InitializeComponent();
        }

        public bool? Display(Category category, Window owner)
        {
            // Set the data context
            DataContext = category;

            // Set the title based on the state of the category
            Title = string.IsNullOrWhiteSpace(category.Name) ? Properties.Resources.CategoryWindowAdd : Properties.Resources.CategoryWindowEdit;

            // Set the window owner
            Owner = owner;

            // Show the dialog and result the result
            return ShowDialog();
        }

        private void HandleOkayButtonClick(object sender, RoutedEventArgs e)
        {
            // Get a list of all explicit binding expressions
            Dictionary<FrameworkElement, BindingExpression> bindingExpressionDictionary = this.GetExplicitBindingExpressions();

            // Get the values as a list
            List<BindingExpression> bindingExpressions = bindingExpressionDictionary.Values.ToList();

            // Loop over each binding expression and clear any existing error
            bindingExpressions.ForEach(Validation.ClearInvalid);

            // Force all explicit bindings to update the source
            bindingExpressions.ForEach(bindingExpression => bindingExpression.UpdateSource());

            // See if there are any errors
            bool hasError = bindingExpressions.Exists(bindingExpression => bindingExpression.HasError);

            // If there was an error then set focus to the bad controls
            if (hasError)
            {
                // Get the first framework element with an error
                FrameworkElement firstErrorElement = bindingExpressionDictionary.First(pair => pair.Value.HasError).Key;

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
