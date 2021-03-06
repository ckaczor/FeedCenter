﻿using System.Collections.Generic;
using System.Windows;
using Common.Wpf.Extensions;
using Common.Internet;
using System.Windows.Controls;
using System.Windows.Data;

internal class UserAgentItem
{
    internal string Caption { get; set; }
    internal string UserAgent { get; set; }
}

namespace FeedCenter.Options
{
    public partial class GeneralOptionsPanel
    {
        public GeneralOptionsPanel()
        {
            InitializeComponent();
        }

        public override void LoadPanel(FeedCenterEntities database)
        {
            base.LoadPanel(database);

            var settings = Properties.Settings.Default;

            StartWithWindowsCheckBox.IsChecked = settings.StartWithWindows;

            LoadBrowserComboBox(BrowserComboBox, settings.Browser);

            LoadUserAgentComboBox(UserAgentComboBox, settings.DefaultUserAgent);
        }

        public override bool ValidatePanel()
        {
            return true;
        }

        public override void SavePanel()
        {
            var settings = Properties.Settings.Default;

            if (StartWithWindowsCheckBox.IsChecked.HasValue &&
                settings.StartWithWindows != StartWithWindowsCheckBox.IsChecked.Value)
                settings.StartWithWindows = StartWithWindowsCheckBox.IsChecked.Value;

            Application.Current.SetStartWithWindows(settings.StartWithWindows);

            settings.Browser = (string) ((ComboBoxItem) BrowserComboBox.SelectedItem).Tag;

            settings.DefaultUserAgent = (string) ((ComboBoxItem) UserAgentComboBox.SelectedItem).Tag;

            var expressions = this.GetBindingExpressions(new[] { UpdateSourceTrigger.Explicit });
            this.UpdateAllSources(expressions);
        }

        public override string CategoryName => Properties.Resources.optionCategoryGeneral;

        private static void LoadBrowserComboBox(ComboBox comboBox, string selected)
        {
            comboBox.SelectedIndex = 0;

            ComboBoxItem selectedItem = null;

            var browsers = Browser.DetectInstalledBrowsers();
            foreach (var browser in browsers)
            {
                var item = new ComboBoxItem { Content = browser.Value.Name, Tag = browser.Key };

                comboBox.Items.Add(item);

                if (browser.Key == selected)
                    selectedItem = item;
            }

            if (selectedItem != null)
                comboBox.SelectedItem = selectedItem;
        }

        private static void LoadUserAgentComboBox(ComboBox comboBox, string selected)
        {
            comboBox.SelectedIndex = 0;

            ComboBoxItem selectedItem = null;

            var userAgents = GetUserAgents();
            foreach (var userAgent in userAgents)
            {
                var item = new ComboBoxItem { Content = userAgent.Caption, Tag = userAgent.UserAgent };

                comboBox.Items.Add(item);

                if (userAgent.UserAgent == selected)
                    selectedItem = item;
            }

            if (selectedItem != null)
                comboBox.SelectedItem = selectedItem;
        }

        private static List<UserAgentItem> GetUserAgents()
        {
            var userAgents = new List<UserAgentItem>
            {
                new UserAgentItem
                {
                    Caption = Properties.Resources.DefaultUserAgentCaption,
                    UserAgent = string.Empty
                },
                new UserAgentItem
                {
                    Caption = "Windows RSS Platform 2.0",
                    UserAgent = "Windows-RSS-Platform/2.0 (MSIE 9.0; Windows NT 6.1)"
                },
                new UserAgentItem
                {
                    Caption = "Feedly 1.0",
                    UserAgent = "Feedly/1.0"
                },
                new UserAgentItem
                {
                    Caption = "curl",
                    UserAgent = "curl/7.47.0"
                }
            };

            return userAgents;
        }
    }
}
