using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ChrisKaczor.InstalledBrowsers;
using FeedCenter.Data;
using FeedCenter.Options;
using FeedCenter.Properties;

namespace FeedCenter
{
    public partial class FeedErrorWindow
    {
        private CollectionViewSource _collectionViewSource;

        public FeedErrorWindow()
        {
            InitializeComponent();
        }

        public void Display(Window owner)
        {
            // Create a view and sort it by name
            _collectionViewSource = new CollectionViewSource { Source = Database.Entities.Feeds };
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

            var feedWindow = new FeedWindow();

            feedWindow.Display(feed, GetWindow(this));
        }

        private void DeleteSelectedFeed()
        {
            if (MessageBox.Show(this, Properties.Resources.ConfirmDeleteFeed, Properties.Resources.ConfirmDeleteTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
                return;

            var feed = (Feed) FeedDataGrid.SelectedItem;

            Database.Entities.SaveChanges(() => Database.Entities.Feeds.Remove(feed));

            SetFeedButtonStates();
        }

        private void SetFeedButtonStates()
        {
            EditFeedButton.IsEnabled = FeedDataGrid.SelectedItem != null;
            DeleteFeedButton.IsEnabled = FeedDataGrid.SelectedItem != null;
            RefreshCurrent.IsEnabled = FeedDataGrid.SelectedItem != null;
            OpenPage.IsEnabled = FeedDataGrid.SelectedItem != null;
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

            Database.Entities.Refresh();

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