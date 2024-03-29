﻿using FeedCenter.Data;
using Microsoft.Win32;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml;

namespace FeedCenter.Options;

public partial class FeedsOptionsPanel
{
    private CollectionViewSource _collectionViewSource;

    public FeedsOptionsPanel(Window parentWindow) : base(parentWindow)
    {
        InitializeComponent();
    }

    public override string CategoryName => Properties.Resources.optionCategoryFeeds;

    public override void LoadPanel()
    {
        base.LoadPanel();

        var collectionViewSource = new CollectionViewSource { Source = Database.Entities.Categories };
        collectionViewSource.SortDescriptions.Add(new SortDescription("SortKey", ListSortDirection.Ascending));
        collectionViewSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        collectionViewSource.IsLiveSortingRequested = true;

        CategoryDataGrid.ItemsSource = collectionViewSource.View;
        CategoryDataGrid.SelectedIndex = 0;
    }

    private void SetFeedButtonStates()
    {
        AddFeedButton.IsEnabled = true;
        EditFeedButton.IsEnabled = FeedDataGrid.SelectedItems.Count == 1;
        DeleteFeedButton.IsEnabled = FeedDataGrid.SelectedItems.Count > 0;
    }

    private void AddFeed()
    {
        var feed = Feed.Create();

        var category = (Category) CategoryDataGrid.SelectedItem;

        feed.CategoryId = category.Id;

        var feedWindow = new FeedWindow();

        var result = feedWindow.Display(feed, Window.GetWindow(this));

        if (!result.HasValue || !result.Value)
            return;

        Database.Entities.SaveChanges(() => Database.Entities.Feeds.Add(feed));

        FeedDataGrid.SelectedItem = feed;

        SetFeedButtonStates();
    }

    private void EditSelectedFeed()
    {
        if (FeedDataGrid.SelectedItem == null)
            return;

        var feed = (Feed) FeedDataGrid.SelectedItem;

        var feedWindow = new FeedWindow();

        feedWindow.Display(feed, Window.GetWindow(this));
    }

    private void DeleteSelectedFeeds()
    {
        if (MessageBox.Show(ParentWindow, Properties.Resources.ConfirmDeleteFeeds, Properties.Resources.ConfirmDeleteTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
            return;

        var selectedItems = new Feed[FeedDataGrid.SelectedItems.Count];

        FeedDataGrid.SelectedItems.CopyTo(selectedItems, 0);

        foreach (var feed in selectedItems)
            Database.Entities.SaveChanges(() => Database.Entities.Feeds.Remove(feed));

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
        DeleteSelectedFeeds();
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
                feed.CategoryId = Database.Entities.Categories.First(c => c.IsDefault).Id;

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

        var selectedId = ((Category) CategoryDataGrid.SelectedItem).Id;

        EditCategoryButton.IsEnabled = CategoryDataGrid.SelectedItem != null &&
                                       selectedId != Database.Entities.DefaultCategory.Id;
        DeleteCategoryButton.IsEnabled = CategoryDataGrid.SelectedItem != null &&
                                         selectedId != Database.Entities.DefaultCategory.Id;
    }

    private void AddCategory()
    {
        var category = new Category();

        var categoryWindow = new CategoryWindow();

        var result = categoryWindow.Display(category, Window.GetWindow(this));

        if (!result.HasValue || !result.Value)
            return;

        Database.Entities.SaveChanges(() => Database.Entities.Categories.Add(category));

        CategoryDataGrid.SelectedItem = category;

        SetCategoryButtonStates();
    }

    private void EditSelectedCategory()
    {
        if (CategoryDataGrid.SelectedItem == null)
            return;

        var category = (Category) CategoryDataGrid.SelectedItem;

        var categoryWindow = new CategoryWindow();

        categoryWindow.Display(category, Window.GetWindow(this));
    }

    private void DeleteSelectedCategory()
    {
        var category = (Category) CategoryDataGrid.SelectedItem;

        if (MessageBox.Show(ParentWindow, string.Format(Properties.Resources.ConfirmDeleteCategory, category.Name), Properties.Resources.ConfirmDeleteTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
            return;

        var defaultCategory = Database.Entities.DefaultCategory;

        foreach (var feed in Database.Entities.Feeds.Where(f => f.CategoryId == category.Id))
            Database.Entities.SaveChanges(() => feed.CategoryId = defaultCategory.Id);

        var index = CategoryDataGrid.SelectedIndex;

        if (index == CategoryDataGrid.Items.Count - 1)
            CategoryDataGrid.SelectedIndex = index - 1;
        else
            CategoryDataGrid.SelectedIndex = index + 1;

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

    private void HandleCategoryDataGridSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_collectionViewSource == null)
        {
            _collectionViewSource = new CollectionViewSource { Source = Database.Entities.Feeds };
            _collectionViewSource.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            _collectionViewSource.Filter += HandleCollectionViewSourceFilter;

            FeedDataGrid.ItemsSource = _collectionViewSource.View;
        }

        _collectionViewSource.View.Refresh();

        if (FeedDataGrid.Items.Count > 0)
            FeedDataGrid.SelectedIndex = 0;

        SetFeedButtonStates();
        SetCategoryButtonStates();
    }

    private void HandleCollectionViewSourceFilter(object sender, FilterEventArgs e)
    {
        var selectedCategory = (Category) CategoryDataGrid.SelectedItem;

        var feed = (Feed) e.Item;

        e.Accepted = feed.CategoryId == selectedCategory.Id;
    }

    private void HandleCategoryDataGridRowDrop(object sender, DragEventArgs e)
    {
        var feedList = (List<Feed>) e.Data.GetData(typeof(List<Feed>));

        var category = (Category) ((DataGridRow) sender).Item;

        foreach (var feed in feedList!)
            Database.Entities.SaveChanges(() => feed.CategoryId = category.Id);

        _collectionViewSource.View.Refresh();

        var dataGridRow = (DataGridRow) sender;

        dataGridRow.FontWeight = FontWeights.Normal;
    }

    private void HandleCategoryDataGridRowDragEnter(object sender, DragEventArgs e)
    {
        var dataGridRow = (DataGridRow) sender;

        dataGridRow.FontWeight = FontWeights.Bold;
    }

    private void HandleCategoryDataGridRowDragLeave(object sender, DragEventArgs e)
    {
        var dataGridRow = (DataGridRow) sender;

        dataGridRow.FontWeight = FontWeights.Normal;
    }

    private void HandleFeedDataGridRowMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        EditSelectedFeed();
    }

    private void HandleMultipleEditClick(object sender, RoutedEventArgs e)
    {
        var bulkFeedWindow = new BulkFeedWindow();
        bulkFeedWindow.Display(Window.GetWindow(this));
    }

    private void HandleFeedDataGridRowPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var selectedItems = FeedDataGrid.SelectedItems.Cast<Feed>().ToList();

        DragDrop.DoDragDrop(FeedDataGrid, selectedItems, DragDropEffects.Move);
    }

    private void HandleCategoryDataGridRowMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (!EditCategoryButton.IsEnabled)
            return;

        EditSelectedCategory();
    }

    private void HandleFeedDataGridSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SetFeedButtonStates();
    }
}