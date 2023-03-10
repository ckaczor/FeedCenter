using FeedCenter.Properties;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Common.Helpers;

namespace FeedCenter
{
    public partial class MainWindow
    {
        private void LoadWindowSettings()
        {
            // Get the last window location
            var windowLocation = Settings.Default.WindowLocation;

            // Set the window into position
            Left = windowLocation.X;
            Top = windowLocation.Y;

            // Get the last window size
            var windowSize = Settings.Default.WindowSize;

            // Set the window into the previous size if it is valid
            if (!windowSize.Width.Equals(0) && !windowSize.Height.Equals(0))
            {
                Width = windowSize.Width;
                Height = windowSize.Height;
            }

            // Set the location of the navigation tray
            switch (Settings.Default.ToolbarLocation)
            {
                case Dock.Top:
                    NameBasedGrid.NameBasedGrid.SetRow(NavigationToolbarTray, "TopToolbarRow");
                    break;
                case Dock.Bottom:
                    NameBasedGrid.NameBasedGrid.SetRow(NavigationToolbarTray, "BottomToolbarRow");
                    break;
            }

            // Load the lock state
            HandleWindowLockState();
        }

        private void SaveWindowSettings()
        {
            // Set the last window location
            Settings.Default.WindowLocation = new Point(Left, Top);

            // Set the last window size
            Settings.Default.WindowSize = new Size(Width, Height);

            // Save the dock on the navigation tray
            Settings.Default.ToolbarLocation = NameBasedGrid.NameBasedGrid.GetRow(NavigationToolbarTray) == "TopToolbarRow" ? Dock.Top : Dock.Bottom;

            // Save settings
            Settings.Default.Save();
        }

        private void HandleWindowLockState()
        {
            // Set the resize mode for the window
            ResizeMode = Settings.Default.WindowLocked ? ResizeMode.NoResize : ResizeMode.CanResize;

            // Show or hide the border
            WindowBorder.BorderBrush = Settings.Default.WindowLocked ? SystemColors.ActiveBorderBrush : Brushes.Transparent;

            // Update the borders
            UpdateBorder();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            // Ditch the worker
            if (_feedReadWorker != null)
            {
                _feedReadWorker.CancelAsync();
                _feedReadWorker.Dispose();
            }

            // Get rid of the timer
            TerminateTimer();

            // Save current window settings
            SaveWindowSettings();

            // Save settings
            _database.SaveChanges(() => Settings.Default.Save());

            // Get rid of the notification icon
            NotificationIcon.Dispose();
        }

        private DelayedMethod _windowStateDelay;
        private void HandleWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _windowStateDelay ??= new DelayedMethod(500, UpdateWindowSettings);

            _windowStateDelay.Reset();
        }

        private void HandleWindowLocationChanged(object sender, EventArgs e)
        {
            _windowStateDelay ??= new DelayedMethod(500, UpdateWindowSettings);

            _windowStateDelay.Reset();
        }

        private void UpdateBorder()
        {
            var windowInteropHelper = new WindowInteropHelper(this);

            var screen = System.Windows.Forms.Screen.FromHandle(windowInteropHelper.Handle);

            var rectangle = new System.Drawing.Rectangle
            {
                X = (int) Left,
                Y = (int) Top,
                Width = (int) Width,
                Height = (int) Height
            };

            var borderThickness = new Thickness();

            if (rectangle.Right != screen.WorkingArea.Right)
                borderThickness.Right = 1;

            if (rectangle.Left != screen.WorkingArea.Left)
                borderThickness.Left = 1;

            if (rectangle.Top != screen.WorkingArea.Top)
                borderThickness.Top = 1;

            if (rectangle.Bottom != screen.WorkingArea.Bottom)
                borderThickness.Bottom = 1;

            WindowBorder.BorderThickness = borderThickness;
        }

        private void UpdateWindowSettings()
        {
            // Save current window settings
            SaveWindowSettings();

            // Update the border
            UpdateBorder();
        }

        private bool _activated;
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            if (_activated)
                return;

            _activated = true;

            // Load the lock state
            HandleWindowLockState();

            // Watch for size and location changes
            SizeChanged += HandleWindowSizeChanged;
            LocationChanged += HandleWindowLocationChanged;

            // Watch for setting changes
            Settings.Default.PropertyChanged += HandlePropertyChanged;
        }
    }
}
