using Common.Update;
using System.Reflection;

namespace FeedCenter.Options
{
    public partial class AboutOptionsPanel
    {
        public AboutOptionsPanel()
        {
            InitializeComponent();
        }

        public override void LoadPanel(FeedCenterEntities database)
        {
            base.LoadPanel(database);

            ApplicationNameLabel.Text = Properties.Resources.ApplicationDisplayName;

            var version = UpdateCheck.LocalVersion.ToString();
            VersionLabel.Text = string.Format(Properties.Resources.Version, version);

            CompanyLabel.Text = ((AssemblyCompanyAttribute) Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false)[0]).Company;
        }

        public override bool ValidatePanel()
        {
            return true;
        }

        public override void SavePanel()
        {
        }

        public override string CategoryName => Properties.Resources.optionCategoryAbout;
    }
}
