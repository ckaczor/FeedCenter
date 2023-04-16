using System;
using System.Collections.Generic;
using System.Windows;

namespace FeedCenter;

public partial class FeedChooserWindow
{
    private string _returnLink;

    public FeedChooserWindow()
    {
        InitializeComponent();
    }

    public string Display(Window owner, List<Tuple<string, string>> rssLinks)
    {
        // Bind to the list
        FeedDataGrid.ItemsSource = rssLinks;
        FeedDataGrid.SelectedIndex = 0;

        // Set the window owner
        Owner = owner;

        ShowDialog();

        return _returnLink;
    }

    private void Save()
    {
        var selectedItem = (Tuple<string, string>) FeedDataGrid.SelectedItem;

        _returnLink = selectedItem.Item1;

        Close();
    }

    private void HandleOkayButtonClick(object sender, RoutedEventArgs e)
    {
        Save();
    }

    private void HandleMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (FeedDataGrid.SelectedItem != null)
        {
            Save();
        }
    }
}