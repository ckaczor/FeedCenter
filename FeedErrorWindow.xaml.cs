using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using FeedCenter.Options;

namespace FeedCenter
{
    public partial class FeedErrorWindow
    {
        public FeedErrorWindow()
        {
            InitializeComponent();
        }

        private FeedCenterEntities _database;
        private CollectionViewSource _collectionViewSource;

        public bool? Display(Window owner)
        {
            _database = new FeedCenterEntities();

            // Create a view and sort it by name
            _collectionViewSource = new CollectionViewSource { Source = _database.AllFeeds };
            _collectionViewSource.Filter += HandleCollectionViewSourceFilter;
            _collectionViewSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            // Bind to the list
            feedDataGrid.ItemsSource = _collectionViewSource.View;
            feedDataGrid.SelectedIndex = 0;

            // Set the window owner
            Owner = owner;

            // Show the dialog and result the result
            return ShowDialog();
        }

        private void HandleCollectionViewSourceFilter(object sender, FilterEventArgs e)
        {
            Feed feed = (Feed) e.Item;

            e.Accepted = (feed.LastReadResult != (int) FeedReadResult.Success);
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
            if (feedDataGrid.SelectedItem == null)
                return;

            Feed feed = (Feed) feedDataGrid.SelectedItem;

            FeedWindow feedWindow = new FeedWindow();

            feedWindow.Display(_database, feed, GetWindow(this));
        }

        private void DeleteSelectedFeed()
        {
            Feed feed = (Feed) feedDataGrid.SelectedItem;

            _database.Feeds.DeleteObject(feed);

            SetFeedButtonStates();
        }

        private void SetFeedButtonStates()
        {
            editFeedButton.IsEnabled = (feedDataGrid.SelectedItem != null);
            deleteFeedButton.IsEnabled = (feedDataGrid.SelectedItem != null);
            refreshCurrent.IsEnabled = (feedDataGrid.SelectedItem != null);
            openPage.IsEnabled = (feedDataGrid.SelectedItem != null);
            openFeed.IsEnabled = (feedDataGrid.SelectedItem != null);
        }

        private void HandleOpenPageButtonClick(object sender, RoutedEventArgs e)
        {
            Feed feed = (Feed) feedDataGrid.SelectedItem;
            BrowserCommon.OpenLink(feed.Link);
        }

        private void HandleOpenFeedButtonClick(object sender, RoutedEventArgs e)
        {
            Feed feed = (Feed) feedDataGrid.SelectedItem;
            BrowserCommon.OpenLink(feed.Source);
        }

        private void HandleOkayButtonClick(object sender, RoutedEventArgs e)
        {
            // Save the actual settings
            _database.SaveChanges();

            DialogResult = true;

            Close();
        }

        private void HandleRefreshCurrentButtonClick(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            Feed feed = (Feed) feedDataGrid.SelectedItem;
            feed.Read(_database, true);

            _collectionViewSource.View.Refresh();

            SetFeedButtonStates();

            Mouse.OverrideCursor = null;
        }
    }
}
