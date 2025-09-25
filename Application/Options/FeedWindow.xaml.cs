using ChrisKaczor.Wpf.Validation;
using System.Windows;

namespace FeedCenter.Options;

public partial class FeedWindow
{
    private readonly FeedCenterEntities _entities;

    public FeedWindow(FeedCenterEntities entities)
    {
        _entities = entities;

        InitializeComponent();
    }

    public bool? Display(Feed feed, Window owner)
    {
        CategoryComboBox.ItemsSource = _entities.Categories;

        DataContext = feed;

        Title = string.IsNullOrWhiteSpace(feed.Link) ? Properties.Resources.FeedWindowAdd : Properties.Resources.FeedWindowEdit;

        Owner = owner;

        return ShowDialog();
    }

    private void HandleOkayButtonClick(object sender, RoutedEventArgs e)
    {
        var transaction = _entities.BeginTransaction();

        if (!this.IsValid(OptionsTabControl))
        {
            transaction.Rollback();
            return;
        }

        transaction.Commit();

        DialogResult = true;

        Close();
    }
}