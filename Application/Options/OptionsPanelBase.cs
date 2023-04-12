using System.Windows.Controls;

namespace FeedCenter.Options;

public class OptionsPanelBase : UserControl
{
    public bool HasLoaded { get; private set; }

    public virtual void LoadPanel()
    {
    }

    public void MarkLoaded()
    {
        HasLoaded = true;
    }

    public virtual string CategoryName => null;
}