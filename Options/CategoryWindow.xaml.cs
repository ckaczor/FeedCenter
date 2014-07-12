using Common.Wpf.Extensions;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

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
            var bindingExpressions = this.GetBindingExpressions(new[] { UpdateSourceTrigger.Explicit });

            // Loop over each binding expression and clear any existing error
            bindingExpressions.ForEach(b => Validation.ClearInvalid(b.BindingExpression));

            // Force all explicit bindings to update the source
            bindingExpressions.ForEach(bindingExpression => bindingExpression.BindingExpression.UpdateSource());

            // See if there are any errors
            var hasError = bindingExpressions.Exists(bindingExpression => bindingExpression.BindingExpression.HasError);

            // If there was an error then set focus to the bad controls
            if (hasError)
            {
                // Get the first framework element with an error
                var firstErrorElement = bindingExpressions.First(b => b.BindingExpression.HasError).FrameworkElement;

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
