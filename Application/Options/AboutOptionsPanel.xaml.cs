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

            applicationNameLabel.Text = Properties.Resources.ApplicationDisplayName;

            string version = UpdateCheck.CurrentVersion.ToString();
            versionLabel.Text = string.Format(Properties.Resources.Version, version);

            companyLabel.Text = ((AssemblyCompanyAttribute) Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false)[0]).Company;
        }

        public override bool ValidatePanel()
        {
            return true;
        }

        public override void SavePanel()
        {
        }

        public override string CategoryName
        {
            get { return Properties.Resources.optionCategoryAbout; }
        }
    }
}
