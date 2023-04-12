using System.Reflection;
using System.Windows;
using ChrisKaczor.ApplicationUpdate;

namespace FeedCenter.Options;

public partial class AboutOptionsPanel
{
    public AboutOptionsPanel(Window parentWindow) : base(parentWindow)
    {
        InitializeComponent();
    }

    public override string CategoryName => Properties.Resources.optionCategoryAbout;

    public override void LoadPanel()
    {
        base.LoadPanel();

        ApplicationNameLabel.Text = Properties.Resources.ApplicationDisplayName;

        var version = UpdateCheck.LocalVersion.ToString();
        VersionLabel.Text = string.Format(Properties.Resources.Version, version);

        CompanyLabel.Text = ((AssemblyCompanyAttribute) Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false)[0]).Company;
    }
}