using System.Windows;
using System.Windows.Controls;

namespace FeedCenter.Options;

public class OptionsPanelBase : UserControl
{
    protected readonly Window ParentWindow;

    protected OptionsPanelBase(Window parentWindow)
    {
        ParentWindow = parentWindow;
    }

    public virtual string CategoryName => null;

    protected bool HasLoaded { get; private set; }

    public virtual void LoadPanel()
    {
    }

    protected void MarkLoaded()
    {
        HasLoaded = true;
    }
}