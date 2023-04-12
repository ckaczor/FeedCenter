using Microsoft.Win32;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml;
using FeedCenter.Data;

namespace FeedCenter.Options
{
    public partial class FeedsOptionsPanel
    {
        public FeedsOptionsPanel()
        {
            InitializeComponent();
        }

        public override void LoadPanel()
        {
            base.LoadPanel();

            var collectionViewSource = new CollectionViewSource { Source = Database.Entities.Categories };
            collectionViewSource.SortDescriptions.Add(new SortDescription("SortKey", ListSortDirection.Ascending));
            collectionViewSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            collectionViewSource.IsLiveSortingRequested = true;

            CategoryListBox.ItemsSource = collectionViewSource.View;
            CategoryListBox.SelectedIndex = 0;
        }

        public override string CategoryName => Properties.Resources.optionCategoryFeeds;

        private void SetFeedButtonStates()
        {
            AddFeedButton.IsEnabled = true;
            EditFeedButton.IsEnabled = FeedListBox.SelectedItem != null;
            DeleteFeedButton.IsEnabled = FeedListBox.SelectedItem != null;
        }

        private void AddFeed()
        {
            var feed = Feed.Create();

            var category = (Category) CategoryListBox.SelectedItem;

            feed.Category = category;

            var feedWindow = new FeedWindow();

            var result = feedWindow.Display(feed, Window.GetWindow(this));

            if (!result.HasValue || !result.Value) 
                return;

            Database.Entities.Feeds.Add(feed);

            FeedListBox.SelectedItem = feed;

            SetFeedButtonStates();
        }

        private void EditSelectedFeed()
        {
            if (FeedListBox.SelectedItem == null)
                return;

            var feed = (Feed) FeedListBox.SelectedItem;

            var feedWindow = new FeedWindow();

            feedWindow.Display(feed, Window.GetWindow(this));
        }

        private void DeleteSelectedFeed()
        {
            var feed = (Feed) FeedListBox.SelectedItem;

            Database.Entities.Feeds.Remove(feed);

            SetFeedButtonStates();
        }

        private void HandleAddFeedButtonClick(object sender, RoutedEventArgs e)
        {
            AddFeed();
        }

        private void HandleEditFeedButtonClick(object sender, RoutedEventArgs e)
        {
            EditSelectedFeed();
        }

        private void HandleDeleteFeedButtonClick(object sender, RoutedEventArgs e)
        {
            DeleteSelectedFeed();
        }

        private void HandleImportButtonClick(object sender, RoutedEventArgs e)
        {
            ImportFeeds();
        }

        private void HandleExportButtonClick(object sender, RoutedEventArgs e)
        {
            ExportFeeds();
        }

        private static void ExportFeeds()
        {
            var saveFileDialog = new SaveFileDialog
            {
                FileName = Properties.Resources.ApplicationName,
                Filter = Properties.Resources.ImportExportFilter,
                FilterIndex = 0,
                OverwritePrompt = true
            };

            var result = saveFileDialog.ShowDialog();

            if (!result.GetValueOrDefault(false))
                return;

            var writerSettings = new XmlWriterSettings
            {
                Indent = true,
                CheckCharacters = true,
                ConformanceLevel = ConformanceLevel.Document
            };

            var xmlWriter = XmlWriter.Create(saveFileDialog.FileName, writerSettings);

            xmlWriter.WriteStartElement("opml");
            xmlWriter.WriteStartElement("body");

            foreach (var feed in Database.Entities.Feeds.OrderBy(feed => feed.Name))
            {
                xmlWriter.WriteStartElement("outline");

                xmlWriter.WriteAttributeString("title", feed.Title);
                xmlWriter.WriteAttributeString("htmlUrl", feed.Link);
                xmlWriter.WriteAttributeString("xmlUrl", feed.Source);

                xmlWriter.WriteEndElement();
            }

            xmlWriter.WriteEndElement();

            xmlWriter.WriteEndElement();

            xmlWriter.Flush();
            xmlWriter.Close();
        }

        private static void ImportFeeds()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = Properties.Resources.ImportExportFilter,
                FilterIndex = 0
            };

            var result = openFileDialog.ShowDialog();

            if (!result.GetValueOrDefault(false))
                return;

            var xmlReaderSettings = new XmlReaderSettings { IgnoreWhitespace = true };

            var xmlReader = XmlReader.Create(openFileDialog.FileName, xmlReaderSettings);

            try
            {
                xmlReader.Read();

                xmlReader.ReadStartElement("opml");
                xmlReader.ReadStartElement("body");

                while (xmlReader.NodeType != XmlNodeType.EndElement)
                {
                    var feed = Feed.Create();
                    feed.Category = Database.Entities.Categories.First(c => c.IsDefault);

                    while (xmlReader.MoveToNextAttribute())
                    {
                        switch (xmlReader.Name.ToLower())
                        {
                            case "title":
                                feed.Title = xmlReader.Value;
                                break;

                            // ReSharper disable once StringLiteralTypo
                            case "htmlurl":
                                feed.Link = xmlReader.Value;
                                break;

                            // ReSharper disable once StringLiteralTypo
                            case "xmlurl":
                                feed.Source = xmlReader.Value;
                                break;

                            case "text":
                                feed.Name = xmlReader.Value;
                                break;
                        }
                    }

                    if (string.IsNullOrEmpty(feed.Name))
                        feed.Name = feed.Title;

                    Database.Entities.Feeds.Add(feed);

                    xmlReader.MoveToElement();

                    xmlReader.Skip();
                }

                xmlReader.ReadEndElement();

                xmlReader.ReadEndElement();
            }
            finally
            {
                xmlReader.Close();
            }
        }

        private void SetCategoryButtonStates()
        {
            AddCategoryButton.IsEnabled = true;

            var selectedId = ((Category) CategoryListBox.SelectedItem).Id;

            EditCategoryButton.IsEnabled = CategoryListBox.SelectedItem != null && selectedId != Database.Entities.DefaultCategory.Id;
            DeleteCategoryButton.IsEnabled = CategoryListBox.SelectedItem != null && selectedId != Database.Entities.DefaultCategory.Id;
        }

        private void AddCategory()
        {
            var category = new Category();

            var categoryWindow = new CategoryWindow();

            var result = categoryWindow.Display(category, Window.GetWindow(this));

            if (!result.HasValue || !result.Value)
                return;

            Database.Entities.SaveChanges(() => Database.Entities.Categories.Add(category));

            CategoryListBox.SelectedItem = category;

            SetCategoryButtonStates();
        }

        private void EditSelectedCategory()
        {
            if (CategoryListBox.SelectedItem == null)
                return;

            var category = (Category) CategoryListBox.SelectedItem;

            var categoryWindow = new CategoryWindow();

            categoryWindow.Display(category, Window.GetWindow(this));
        }

        private void DeleteSelectedCategory()
        {
            var defaultCategory = Database.Entities.DefaultCategory;

            var category = (Category) CategoryListBox.SelectedItem;

            category.Feeds?.ToList().ForEach(feed => feed.Category = defaultCategory);

            var index = CategoryListBox.SelectedIndex;

            if (index == CategoryListBox.Items.Count - 1)
                CategoryListBox.SelectedIndex = index - 1;
            else
                CategoryListBox.SelectedIndex = index + 1;

            Database.Entities.SaveChanges(() => Database.Entities.Categories.Remove(category));

            SetCategoryButtonStates();
        }

        private void HandleAddCategoryButtonClick(object sender, RoutedEventArgs e)
        {
            AddCategory();
        }

        private void HandleEditCategoryButtonClick(object sender, RoutedEventArgs e)
        {
            EditSelectedCategory();
        }

        private void HandleDeleteCategoryButtonClick(object sender, RoutedEventArgs e)
        {
            DeleteSelectedCategory();
        }

        private CollectionViewSource _collectionViewSource;

        private void HandleCategoryListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_collectionViewSource == null)
            {
                _collectionViewSource = new CollectionViewSource { Source = Database.Entities.Feeds };
                _collectionViewSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                _collectionViewSource.Filter += HandleCollectionViewSourceFilter;

                FeedListBox.ItemsSource = _collectionViewSource.View;
            }

            _collectionViewSource.View.Refresh();

            if (FeedListBox.Items.Count > 0)
                FeedListBox.SelectedIndex = 0;

            SetFeedButtonStates();
            SetCategoryButtonStates();
        }

        private void HandleCollectionViewSourceFilter(object sender, FilterEventArgs e)
        {
            var selectedCategory = (Category) CategoryListBox.SelectedItem;

            var feed = (Feed) e.Item;

            e.Accepted = feed.Category.Id == selectedCategory.Id;
        }

        private void CategoryListBox_Drop(object sender, DragEventArgs e)
        {
            var feedList = (List<Feed>) e.Data.GetData(typeof(List<Feed>));

            var category = (Category) ((DataGridRow) sender).Item;

            foreach (var feed in feedList!)
                feed.Category = category;

            _collectionViewSource.View.Refresh();

            var dataGridRow = (DataGridRow) sender;

            dataGridRow.FontWeight = FontWeights.Normal;
        }

        private void HandleListBoxItemPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            var selectedItems = FeedListBox.SelectedItems.Cast<Feed>().ToList();

            DragDrop.DoDragDrop(FeedListBox, selectedItems, DragDropEffects.Move);
        }

        private void CategoryListBox_DragEnter(object sender, DragEventArgs e)
        {
            var dataGridRow = (DataGridRow) sender;

            dataGridRow.FontWeight = FontWeights.Bold;
        }

        private void CategoryListBox_DragLeave(object sender, DragEventArgs e)
        {
            var dataGridRow = (DataGridRow) sender;

            dataGridRow.FontWeight = FontWeights.Normal;
        }

        private void HandleListBoxItemMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            EditSelectedFeed();
        }

        private void HandleMultipleEditClick(object sender, RoutedEventArgs e)
        {
            var bulkFeedWindow = new BulkFeedWindow();
            bulkFeedWindow.Display(Window.GetWindow(this));
        }

        private void HandleFeedListPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Get the object that was clicked on
            var originalSource = (DependencyObject) e.OriginalSource;

            // Look for a row that contains the object
            var dataGridRow = (DataGridRow) FeedListBox.ContainerFromElement(originalSource);

            // If the selection already contains this row then ignore it
            if (dataGridRow != null && FeedListBox.SelectedItems.Contains(dataGridRow.Item))
                e.Handled = true;
        }

        private void CategoryListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!EditCategoryButton.IsEnabled)
                return;

            EditSelectedCategory();
        }
    }
}