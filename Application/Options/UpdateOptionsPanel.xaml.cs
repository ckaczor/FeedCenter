﻿using ChrisKaczor.ApplicationUpdate;
using FeedCenter.Properties;
using System.Windows;

namespace FeedCenter.Options;

public partial class UpdateOptionsPanel
{
    public UpdateOptionsPanel(Window parentWindow) : base(parentWindow)
    {
        InitializeComponent();
    }

    public override string CategoryName => Properties.Resources.optionCategoryUpdate;

    private void HandleCheckVersionNowButtonClick(object sender, RoutedEventArgs e)
    {
        UpdateCheck.DisplayUpdateInformation(true);
    }

    private void OnSaveSettings(object sender, RoutedEventArgs e)
    {
        SaveSettings();
    }

    private void SaveSettings()
    {
        if (!HasLoaded) return;

        Settings.Default.Save();
    }
}