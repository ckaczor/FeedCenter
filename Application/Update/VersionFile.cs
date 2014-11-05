using Common.Debug;
using FeedCenter.Properties;
using System;
using System.Xml.Linq;

namespace FeedCenter.Update
{
    public class VersionFile
    {
        public Version Version { get; set; }
        public string InstallFile { get; set; }
        public DateTime InstallCreated { get; set; }

        public static VersionFile Load()
        {
            try
            {
                var document = XDocument.Load(Settings.Default.VersionLocation + Settings.Default.VersionFile);

                var versionInformationElement = document.Element("versionInformation");

                if (versionInformationElement == null)
                    return null;

                var versionElement = versionInformationElement.Element("version");
                var installFileElement = versionInformationElement.Element("installFile");
                var installCreatedElement = versionInformationElement.Element("installCreated");

                if (versionElement == null || installFileElement == null || installCreatedElement == null)
                    return null;

                var versionFile = new VersionFile
                {
                    Version = Version.Parse(versionElement.Value),
                    InstallFile = installFileElement.Value,
                    InstallCreated = DateTime.Parse(installCreatedElement.Value)
                };

                return versionFile;
            }
            catch (Exception exception)
            {
                Tracer.WriteException(exception);

                return null;
            }
        }
    }
}
