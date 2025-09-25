using System.Windows;

namespace FeedCenter.Options;

public partial class AboutOptionsPanel
{
    public AboutOptionsPanel(Window parentWindow, FeedCenterEntities entities) : base(parentWindow, entities)
    {
        InitializeComponent();
    }

    public override string CategoryName => Properties.Resources.optionCategoryAbout;
}