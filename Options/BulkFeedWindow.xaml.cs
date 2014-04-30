using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using Common.Wpf;

namespace FeedCenter.Options
{
    public partial class BulkFeedWindow
    {
        private List<CheckedListItem<Feed>> _checkedListBoxItems;
        private CollectionViewSource _collectionViewSource;

        public BulkFeedWindow()
        {
            InitializeComponent();
        }

        public bool? Display(Window window, FeedCenterEntities database)
        {
            _checkedListBoxItems = new List<CheckedListItem<Feed>>();

            foreach (var feed in database.AllFeeds)
                _checkedListBoxItems.Add(new CheckedListItem<Feed> { Item = feed });

            _collectionViewSource = new CollectionViewSource { Source = _checkedListBoxItems };
            _collectionViewSource.SortDescriptions.Add(new SortDescription("Item.Name", ListSortDirection.Ascending));
            _collectionViewSource.Filter += HandleCollectionViewSourceFilter;

            filteredFeedsList.ItemsSource = _collectionViewSource.View;

            Owner = window;

            return ShowDialog();
        }

        void HandleCollectionViewSourceFilter(object sender, FilterEventArgs e)
        {
            CheckedListItem<Feed> checkedListBoxItem = (CheckedListItem<Feed>) e.Item;

            Feed feed = checkedListBoxItem.Item;

            e.Accepted = feed.Link.Contains(feedLinkFilterText.Text);
        }

        private void HandleFilterTextChanged(object sender, TextChangedEventArgs e)
        {
            _collectionViewSource.View.Refresh();
        }

        private void HandleOkButtonClick(object sender, RoutedEventArgs e)
        {
            foreach (var item in _checkedListBoxItems.Where(i => i.IsChecked))
            {
                if (openComboBox.IsEnabled)
                    item.Item.MultipleOpenAction = (int) ((ComboBoxItem) openComboBox.SelectedItem).Tag;
            }

            DialogResult = true;
            Close();
        }

        private void HandleSelectAll(object sender, RoutedEventArgs e)
        {
            foreach (var viewItem in _collectionViewSource.View)
            {
                var checkedListItem = (CheckedListItem<Feed>) viewItem;

                checkedListItem.IsChecked = true;
            }
        }

        private void HandleSelectNone(object sender, RoutedEventArgs e)
        {
            foreach (var viewItem in _collectionViewSource.View)
            {
                var checkedListItem = (CheckedListItem<Feed>) viewItem;

                checkedListItem.IsChecked = false;
            }
        }

        private void HandleSelectInvert(object sender, RoutedEventArgs e)
        {
            foreach (var viewItem in _collectionViewSource.View)
            {
                var checkedListItem = (CheckedListItem<Feed>) viewItem;

                checkedListItem.IsChecked = !checkedListItem.IsChecked;
            }
        }

        private void HandleGridMouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            openLabel.IsEnabled = !openLabel.IsEnabled;
            openComboBox.IsEnabled = !openComboBox.IsEnabled;
        }
    }
}
