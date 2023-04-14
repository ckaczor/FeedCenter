using JetBrains.Annotations;
using System.Windows;
using System.Windows.Controls;

namespace FeedCenter.Options;

public class MarginSetter
{
    private static Thickness GetLastItemMargin(DependencyObject obj)
    {
        return (Thickness) obj.GetValue(LastItemMarginProperty);
    }

    [UsedImplicitly]
    public static Thickness GetMargin(DependencyObject obj)
    {
        return (Thickness) obj.GetValue(MarginProperty);
    }

    private static void MarginChangedCallback(object sender, DependencyPropertyChangedEventArgs e)
    {
        // Make sure this is put on a panel
        if (sender is not Panel panel)
            return;

        // Avoid duplicate registrations
        panel.Loaded -= OnPanelLoaded;
        panel.Loaded += OnPanelLoaded;

        if (panel.IsLoaded)
        {
            OnPanelLoaded(panel, null);
        }
    }

    private static void OnPanelLoaded(object sender, RoutedEventArgs e)
    {
        var panel = (Panel) sender;

        // Go over the children and set margin for them:
        for (var i = 0; i < panel.Children.Count; i++)
        {
            var child = panel.Children[i];

            if (child is not FrameworkElement fe)
                continue;

            var isLastItem = i == panel.Children.Count - 1;
            fe.Margin = isLastItem ? GetLastItemMargin(panel) : GetMargin(panel);
        }
    }

    [UsedImplicitly]
    public static void SetLastItemMargin(DependencyObject obj, Thickness value)
    {
        obj.SetValue(LastItemMarginProperty, value);
    }

    [UsedImplicitly]
    public static void SetMargin(DependencyObject obj, Thickness value)
    {
        obj.SetValue(MarginProperty, value);
    }

    // Using a DependencyProperty as the backing store for Margin.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty MarginProperty =
        DependencyProperty.RegisterAttached("Margin", typeof(Thickness), typeof(MarginSetter),
            new UIPropertyMetadata(new Thickness(), MarginChangedCallback));

    public static readonly DependencyProperty LastItemMarginProperty =
        DependencyProperty.RegisterAttached("LastItemMargin", typeof(Thickness), typeof(MarginSetter),
            new UIPropertyMetadata(new Thickness(), MarginChangedCallback));
}