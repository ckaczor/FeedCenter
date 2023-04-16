using ChrisKaczor.Wpf.Validation;
using FeedCenter.Data;
using System.Windows;

namespace FeedCenter.Options;

public partial class FeedWindow
{
    public FeedWindow()
    {
        InitializeComponent();
    }

    public bool? Display(Feed feed, Window owner)
    {
        CategoryComboBox.ItemsSource = Database.Entities.Categories;

        DataContext = feed;

        Title = string.IsNullOrWhiteSpace(feed.Link) ? Properties.Resources.FeedWindowAdd : Properties.Resources.FeedWindowEdit;

        Owner = owner;

        return ShowDialog();
    }

    private void HandleOkayButtonClick(object sender, RoutedEventArgs e)
    {
        var transaction = Database.Entities.BeginTransaction();

        if (!this.IsValid(OptionsTabControl))
        {
            transaction.Rollback();
            return;
        }

        transaction.Commit();
        Database.Entities.Refresh();

        DialogResult = true;

        Close();
    }
}