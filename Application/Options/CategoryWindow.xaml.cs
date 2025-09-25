using ChrisKaczor.Wpf.Validation;
using System.Windows;

namespace FeedCenter.Options;

public partial class CategoryWindow
{
    private readonly FeedCenterEntities _entities;

    public CategoryWindow(FeedCenterEntities entities)
    {
        _entities = entities;

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
        var transaction = _entities.BeginTransaction();

        if (!this.IsValid())
        {
            transaction.Rollback();
            return;
        }

        transaction.Commit();

        // Dialog is good
        DialogResult = true;

        // Close the dialog
        Close();
    }
}