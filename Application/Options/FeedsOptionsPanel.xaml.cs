using Microsoft.Win32;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml;

namespace FeedCenter.Options
{
    public partial class FeedsOptionsPanel
    {
        #region Constructor

        public FeedsOptionsPanel()
        {
            InitializeComponent();
        }

        #endregion

        #region OptionsPanelBase overrides

        public override void LoadPanel(FeedCenterEntities database)
        {
            base.LoadPanel(database);

            var collectionViewSource = new CollectionViewSource { Source = Database.AllCategories };
            collectionViewSource.SortDescriptions.Add(new SortDescription("SortKey", ListSortDirection.Ascending));
            collectionViewSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            categoryListBox.ItemsSource = collectionViewSource.View;
            categoryListBox.SelectedIndex = 0;
        }

        public override bool ValidatePanel()
        {
            return true;
        }

        public override void SavePanel()
        { }

        public override string CategoryName
        {
            get { return Properties.Resources.optionCategoryFeeds; }
        }

        #endregion

        #region Feed list management

        private void SetFeedButtonStates()
        {
            addFeedButton.IsEnabled = true;
            editFeedButton.IsEnabled = (feedListBox.SelectedItem != null);
            deleteFeedButton.IsEnabled = (feedListBox.SelectedItem != null);
        }

        private void AddFeed()
        {
            var feed = Feed.Create();

            var feedWindow = new FeedWindow();

            var result = feedWindow.Display(Database, feed, Window.GetWindow(this));

            if (result.HasValue && result.Value)
            {
                Database.Feeds.Add(feed);

                feedListBox.SelectedItem = feed;

                SetFeedButtonStates();
            }
        }

        private void EditSelectedFeed()
        {
            if (feedListBox.SelectedItem == null)
                return;

            var feed = (Feed) feedListBox.SelectedItem;

            var feedWindow = new FeedWindow();

            feedWindow.Display(Database, feed, Window.GetWindow(this));
        }

        private void DeleteSelectedFeed()
        {
            var feed = (Feed) feedListBox.SelectedItem;

            Database.Feeds.Remove(feed);

            SetFeedButtonStates();
        }

        #endregion

        #region Feed event handlers

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

        #endregion

        #region Feed import and export

        private void ExportFeeds()
        {
            // Setup the save file dialog
            var saveFileDialog = new SaveFileDialog
            {
                Filter = Properties.Resources.ImportExportFilter,
                FilterIndex = 0,
                OverwritePrompt = true
            };

            var result = saveFileDialog.ShowDialog();

            if (!result.GetValueOrDefault(false))
                return;

            // Setup the writer settings
            var writerSettings = new XmlWriterSettings
            {
                Indent = true,
                CheckCharacters = true,
                ConformanceLevel = ConformanceLevel.Document
            };

            // Create an XML writer for the file chosen
            var xmlWriter = XmlWriter.Create(saveFileDialog.FileName, writerSettings);

            // Start the opml element
            xmlWriter.WriteStartElement("opml");

            // Start the body element
            xmlWriter.WriteStartElement("body");

            // Loop over each feed
            foreach (var feed in Database.Feeds.OrderBy(feed => feed.Name))
            {
                // Start the outline element
                xmlWriter.WriteStartElement("outline");

                // Write the title
                xmlWriter.WriteAttributeString("title", feed.Title);

                // Write the HTML link
                xmlWriter.WriteAttributeString("htmlUrl", feed.Link);

                // Write the XML link
                xmlWriter.WriteAttributeString("xmlUrl", feed.Source);

                // End the outline element
                xmlWriter.WriteEndElement();
            }

            // End the body element
            xmlWriter.WriteEndElement();

            // End the opml element
            xmlWriter.WriteEndElement();

            // Flush and close the writer
            xmlWriter.Flush();
            xmlWriter.Close();
        }

        private void ImportFeeds()
        {
            // Setup the open file dialog
            var openFileDialog = new OpenFileDialog
            {
                Filter = Properties.Resources.ImportExportFilter,
                FilterIndex = 0
            };

            var result = openFileDialog.ShowDialog();

            if (!result.GetValueOrDefault(false))
                return;

            // Setup the reader settings
            var xmlReaderSettings = new XmlReaderSettings { IgnoreWhitespace = true };

            // Create an XML reader for the file chosen
            var xmlReader = XmlReader.Create(openFileDialog.FileName, xmlReaderSettings);

            try
            {
                // Read the first node
                xmlReader.Read();

                // Read the OPML node
                xmlReader.ReadStartElement("opml");

                // Read the body node
                xmlReader.ReadStartElement("body");

                // Read all outline nodes
                while (xmlReader.NodeType != XmlNodeType.EndElement)
                {
                    // Create a new feed
                    var feed = Feed.Create();
                    feed.Category = Database.Categories.ToList().First(c => c.IsDefault);

                    // Loop over all attributes
                    while (xmlReader.MoveToNextAttribute())
                    {
                        // Handle the attibute
                        switch (xmlReader.Name.ToLower())
                        {
                            case "title":
                                feed.Title = xmlReader.Value;
                                break;

                            case "htmlurl":
                                feed.Link = xmlReader.Value;
                                break;

                            case "xmlurl":
                                feed.Source = xmlReader.Value;
                                break;

                            case "text":
                                feed.Name = xmlReader.Value;
                                break;
                        }
                    }

                    // Fill in defaults for optional fields
                    if (string.IsNullOrEmpty(feed.Name))
                        feed.Name = feed.Title;

                    // Add the feed to the main list
                    Database.Feeds.Add(feed);

                    // Move back to the element node
                    xmlReader.MoveToElement();

                    // Skip to the next node
                    xmlReader.Skip();
                }

                // End the body node
                xmlReader.ReadEndElement();

                // End the OPML node
                xmlReader.ReadEndElement();
            }
            finally
            {
                xmlReader.Close();
            }
        }

        #endregion

        #region Category list management

        private void SetCategoryButtonStates()
        {
            addCategoryButton.IsEnabled = true;
            editCategoryButton.IsEnabled = (categoryListBox.SelectedItem != null);
            deleteCategoryButton.IsEnabled = (categoryListBox.SelectedItem != null);
        }

        private void AddCategory()
        {
            var category = Category.Create();

            var categoryWindow = new CategoryWindow();

            var result = categoryWindow.Display(category, Window.GetWindow(this));

            if (result.HasValue && result.Value)
            {
                Database.Categories.Add(category);

                categoryListBox.SelectedItem = category;

                SetCategoryButtonStates();
            }
        }

        private void EditSelectedCategory()
        {
            if (categoryListBox.SelectedItem == null)
                return;

            var category = (Category) categoryListBox.SelectedItem;

            var categoryWindow = new CategoryWindow();

            categoryWindow.Display(category, Window.GetWindow(this));
        }

        private void DeleteSelectedCategory()
        {
            var category = (Category) categoryListBox.SelectedItem;

            Database.Categories.Remove(category);

            SetCategoryButtonStates();
        }

        #endregion

        #region Category event handlers

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

        #endregion

        private CollectionViewSource _collectionViewSource;

        private void HandleCategoryListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_collectionViewSource == null)
            {
                _collectionViewSource = new CollectionViewSource { Source = Database.AllFeeds };
                _collectionViewSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                _collectionViewSource.Filter += HandleCollectionViewSourceFilter;

                feedListBox.ItemsSource = _collectionViewSource.View;
            }

            _collectionViewSource.View.Refresh();

            if (feedListBox.Items.Count > 0)
                feedListBox.SelectedIndex = 0;

            SetFeedButtonStates();
        }

        private void HandleCollectionViewSourceFilter(object sender, FilterEventArgs e)
        {
            var selectedCategory = (Category) categoryListBox.SelectedItem;

            var feed = (Feed) e.Item;

            e.Accepted = (feed.Category == selectedCategory);
        }

        private void HandleTextBlockDrop(object sender, DragEventArgs e)
        {
            var feedList = (List<Feed>) e.Data.GetData(typeof(List<Feed>));

            var category = (Category) ((DataGridRow) sender).Item;

            foreach (var feed in feedList)
                feed.Category = category;

            _collectionViewSource.View.Refresh();

            //textBlock.TextDecorations = null;
        }

        private void HandleListBoxItemPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var selectedItems = feedListBox.SelectedItems.Cast<Feed>().ToList();

                DragDrop.DoDragDrop(feedListBox, selectedItems, DragDropEffects.Move);
            }
        }

        private void HandleTextBlockDragEnter(object sender, DragEventArgs e)
        {
            var dataGridRow = (DataGridRow) sender;

            dataGridRow.FontWeight = FontWeights.Bold;
        }

        private void HandleTextBlockDragLeave(object sender, DragEventArgs e)
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
            bulkFeedWindow.Display(Window.GetWindow(this), Database);
        }
    }
}
