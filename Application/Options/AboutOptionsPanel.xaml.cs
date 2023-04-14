using System.Windows;

namespace FeedCenter.Options;

public partial class AboutOptionsPanel
{
    public AboutOptionsPanel(Window parentWindow) : base(parentWindow)
    {
        InitializeComponent();
    }

    public override string CategoryName => Properties.Resources.optionCategoryAbout;
}