using ChrisKaczor.ApplicationUpdate;
using System.Reflection;

namespace FeedCenter.Options;

public partial class AboutOptionsPanel
{
    public AboutOptionsPanel()
    {
        InitializeComponent();
    }

    public override void LoadPanel()
    {
        base.LoadPanel();

        ApplicationNameLabel.Text = Properties.Resources.ApplicationDisplayName;

        var version = UpdateCheck.LocalVersion.ToString();
        VersionLabel.Text = string.Format(Properties.Resources.Version, version);

        CompanyLabel.Text = ((AssemblyCompanyAttribute) Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false)[0]).Company;
    }

    public override string CategoryName => Properties.Resources.optionCategoryAbout;
}