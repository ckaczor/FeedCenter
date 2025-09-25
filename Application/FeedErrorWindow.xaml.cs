using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ChrisKaczor.InstalledBrowsers;
using FeedCenter.Options;
using FeedCenter.Properties;

namespace FeedCenter;

public partial class FeedErrorWindow
{
    private CollectionViewSource _collectionViewSource;
    private readonly FeedCenterEntities _entities = new();

    public FeedErrorWindow()
    {
        InitializeComponent();
    }

    public void Display(Window owner)
    {
        // Create a view and sort it by name
        _collectionViewSource = new CollectionViewSource { Source = _entities.Feeds };
        _collectionViewSource.Filter += HandleCollectionViewSourceFilter;
        _collectionViewSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

        // Bind to the list
        FeedDataGrid.ItemsSource = _collectionViewSource.View;
        FeedDataGrid.SelectedIndex = 0;

        // Set the window owner
        Owner = owner;

        // Show the dialog and result the result
        ShowDialog();
    }

    private static void HandleCollectionViewSourceFilter(object sender, FilterEventArgs e)
    {
        var feed = (Feed) e.Item;

        e.Accepted = feed.LastReadResult != FeedReadResult.Success;
    }

    private void HandleEditFeedButtonClick(object sender, RoutedEventArgs e)
    {
        EditSelectedFeed();
    }

    private void HandleDeleteFeedButtonClick(object sender, RoutedEventArgs e)
    {
        DeleteSelectedFeed();
    }

    private void EditSelectedFeed()
    {
        if (FeedDataGrid.SelectedItem == null)
            return;

        var feed = (Feed) FeedDataGrid.SelectedItem;

        var feedWindow = new FeedWindow(_entities);

        feedWindow.Display(feed, GetWindow(this));
    }

    private void DeleteSelectedFeed()
    {
        if (MessageBox.Show(this, Properties.Resources.ConfirmDeleteFeed, Properties.Resources.ConfirmDeleteTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
            return;

        var feed = (Feed) FeedDataGrid.SelectedItem;

        _entities.SaveChanges(() => _entities.Feeds.Remove(feed));

        SetFeedButtonStates();
    }

    private void SetFeedButtonStates()
    {
        var feed = FeedDataGrid.SelectedItem as Feed;

        EditFeedButton.IsEnabled = feed != null;
        DeleteFeedButton.IsEnabled = feed != null;
        RefreshCurrent.IsEnabled = feed != null;
        OpenPage.IsEnabled = feed != null && !string.IsNullOrEmpty(feed.Link);
        OpenFeed.IsEnabled = FeedDataGrid.SelectedItem != null;
    }

    private void HandleOpenPageButtonClick(object sender, RoutedEventArgs e)
    {
        var feed = (Feed) FeedDataGrid.SelectedItem;
        InstalledBrowser.OpenLink(Settings.Default.Browser, feed.Link);
    }

    private void HandleOpenFeedButtonClick(object sender, RoutedEventArgs e)
    {
        var feed = (Feed) FeedDataGrid.SelectedItem;
        InstalledBrowser.OpenLink(Settings.Default.Browser, feed.Source);
    }

    private async void HandleRefreshCurrentButtonClick(object sender, RoutedEventArgs e)
    {
        IsEnabled = false;
        Mouse.OverrideCursor = Cursors.Wait;

        var feedId = ((Feed) FeedDataGrid.SelectedItem).Id;

        await Task.Run(() =>
        {
            var entities = new FeedCenterEntities();

            var feed = entities.Feeds.First(f => f.Id == feedId);

            entities.SaveChanges(() => feed.Read(true));
        });

        var selectedIndex = FeedDataGrid.SelectedIndex;

        _collectionViewSource.View.Refresh();

        if (selectedIndex >= FeedDataGrid.Items.Count)
            FeedDataGrid.SelectedIndex = FeedDataGrid.Items.Count - 1;
        else
            FeedDataGrid.SelectedIndex = selectedIndex;

        SetFeedButtonStates();

        Mouse.OverrideCursor = null;
        IsEnabled = true;
    }

    private void FeedDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        SetFeedButtonStates();
    }
}