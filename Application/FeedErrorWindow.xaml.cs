using CKaczor.InstalledBrowsers;
using FeedCenter.Data;
using FeedCenter.Options;
using FeedCenter.Properties;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

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
            _database = Database.Entities;

            // Create a view and sort it by name
            _collectionViewSource = new CollectionViewSource { Source = _database.Feeds };
            _collectionViewSource.Filter += HandleCollectionViewSourceFilter;
            _collectionViewSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            // Bind to the list
            FeedDataGrid.ItemsSource = _collectionViewSource.View;
            FeedDataGrid.SelectedIndex = 0;

            // Set the window owner
            Owner = owner;

            // Show the dialog and result the result
            return ShowDialog();
        }

        private void HandleCollectionViewSourceFilter(object sender, FilterEventArgs e)
        {
            var feed = (Feed) e.Item;

            e.Accepted = (feed.LastReadResult != FeedReadResult.Success);
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

            var feedWindow = new FeedWindow();

            feedWindow.Display(_database, feed, GetWindow(this));
        }

        private void DeleteSelectedFeed()
        {
            var feed = (Feed) FeedDataGrid.SelectedItem;

            _database.Feeds.Remove(feed);

            SetFeedButtonStates();
        }

        private void SetFeedButtonStates()
        {
            EditFeedButton.IsEnabled = (FeedDataGrid.SelectedItem != null);
            DeleteFeedButton.IsEnabled = (FeedDataGrid.SelectedItem != null);
            RefreshCurrent.IsEnabled = (FeedDataGrid.SelectedItem != null);
            OpenPage.IsEnabled = (FeedDataGrid.SelectedItem != null);
            OpenFeed.IsEnabled = (FeedDataGrid.SelectedItem != null);
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

        private void HandleOkayButtonClick(object sender, RoutedEventArgs e)
        {
            // Save the actual settings
            _database.SaveChanges(() => { });

            DialogResult = true;

            Close();
        }

        private void HandleRefreshCurrentButtonClick(object sender, RoutedEventArgs e)
        {
            IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;

            var feed = (Feed) FeedDataGrid.SelectedItem;

            _database.SaveChanges(() => feed.Read(_database, true));

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
    }
}