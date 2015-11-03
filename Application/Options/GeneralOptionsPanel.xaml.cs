using System.Windows;
using Common.Wpf.Extensions;

namespace FeedCenter.Options
{
    public partial class GeneralOptionsPanel
    {
        public GeneralOptionsPanel()
        {
            InitializeComponent();
        }

        public override void LoadPanel(FeedCenterEntities database)
        {
            base.LoadPanel(database);

            var settings = Properties.Settings.Default;

            StartWithWindowsCheckBox.IsChecked = settings.StartWithWindows;
        }

        public override bool ValidatePanel()
        {
            return true;
        }

        public override void SavePanel()
        {
            var settings = Properties.Settings.Default;

            if (StartWithWindowsCheckBox.IsChecked.HasValue && settings.StartWithWindows != StartWithWindowsCheckBox.IsChecked.Value)
                settings.StartWithWindows = StartWithWindowsCheckBox.IsChecked.Value;

            Application.Current.SetStartWithWindows(settings.StartWithWindows);
        }

        public override string CategoryName => Properties.Resources.optionCategoryGeneral;
    }
}
