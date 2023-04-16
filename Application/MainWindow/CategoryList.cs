using FeedCenter.Properties;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FeedCenter;

public partial class MainWindow
{
    private void DisplayCategory()
    {
        CategoryLabel.Text = string.Format(Properties.Resources.CategoryFilterHeader, _currentCategory == null ? Properties.Resources.AllCategory : _currentCategory.Name);
    }

    private void HandleCategoryButtonClick(object sender, RoutedEventArgs e)
    {
        // Create a new context menu
        var contextMenu = new ContextMenu();

        // Create the "all" menu item
        var menuItem = new MenuItem
        {
            Header = Properties.Resources.AllCategory,
            Tag = null,

            // Set the current item to bold
            FontWeight = _currentCategory == null ? FontWeights.Bold : FontWeights.Normal
        };

        // Handle the click
        menuItem.Click += HandleCategoryMenuItemClick;

        // Add the item to the list
        contextMenu.Items.Add(menuItem);

        // Loop over each feed
        foreach (var category in _database.Categories.OrderBy(category => category.Name))
        {
            // Create a menu item
            menuItem = new MenuItem
            {
                Header = category.Name,
                Tag = category,

                // Set the current item to bold
                FontWeight = category.Id == _currentCategory?.Id ? FontWeights.Bold : FontWeights.Normal
            };

            // Handle the click
            menuItem.Click += HandleCategoryMenuItemClick;

            // Add the item to the list
            contextMenu.Items.Add(menuItem);
        }

        // Set the context menu placement to this button
        contextMenu.PlacementTarget = this;

        // Open the context menu
        contextMenu.IsOpen = true;
    }

    private void HandleCategoryMenuItemClick(object sender, RoutedEventArgs e)
    {
        // Get the menu item clicked
        var menuItem = (MenuItem) sender;

        // Get the category from the menu item tab
        var category = (Category) menuItem.Tag;

        // If the category changed then reset the current feed to the first in the category
        if (_currentCategory?.Id != category?.Id)
        {
            _currentFeed = category == null ? _database.Feeds.FirstOrDefault() : category.Feeds.FirstOrDefault();
        }

        // Set the current category
        _currentCategory = category;

        // Get the current feed list to match the category
        _feedList = _currentCategory == null ? _database.Feeds : _database.Feeds.Where(feed => feed.CategoryId == _currentCategory.Id);

        // Refresh the feed index
        _feedIndex = -1;

        // Get the first feed
        NextFeed();

        // Update the feed timestamp
        _lastFeedDisplay = DateTime.Now;

        // Update the display
        DisplayCategory();
        DisplayFeed();

        Settings.Default.LastCategoryID = _currentCategory?.Id.ToString() ?? string.Empty;
    }
}