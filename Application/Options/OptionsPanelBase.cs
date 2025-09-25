using System.Windows;
using System.Windows.Controls;

namespace FeedCenter.Options;

public class OptionsPanelBase : UserControl
{
    protected readonly Window ParentWindow;
    protected readonly FeedCenterEntities Entities;

    protected OptionsPanelBase(Window parentWindow, FeedCenterEntities entities)
    {
        ParentWindow = parentWindow;
        Entities = entities;
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