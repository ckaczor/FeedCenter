using System.Windows;
using ChrisKaczor.Wpf.Validation;
using FeedCenter.Data;

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
            Title = string.IsNullOrWhiteSpace(category.Name)
                ? Properties.Resources.CategoryWindowAdd
                : Properties.Resources.CategoryWindowEdit;

            // Set the window owner
            Owner = owner;

            // Show the dialog and result the result
            return ShowDialog();
        }

        private void HandleOkayButtonClick(object sender, RoutedEventArgs e)
        {
            var transaction = Database.Entities.BeginTransaction();

            if (!this.IsValid())
            {
                transaction.Rollback();
                return;
            }

            transaction.Commit();
            Database.Entities.Refresh();

            // Dialog is good
            DialogResult = true;

            // Close the dialog
            Close();
        }
    }
}