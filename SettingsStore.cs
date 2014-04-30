using System.Collections.Generic;
using System.Linq;

namespace FeedCenter
{
    public static class SettingsStore
    {
        public static object OpenDataStore()
        {
            FeedCenterEntities entities = new FeedCenterEntities();

            return entities.DatabaseExists() ? entities : null;
        }

        public static void CloseDataStore(object dataStore)
        {
            FeedCenterEntities entities = (FeedCenterEntities) dataStore;

            if (entities == null)
                return;

            entities.SaveChanges();

            entities.Dispose();
        }

        public static string GetSettingValue(object dataStore, string name, string version)
        {
            FeedCenterEntities entities = (FeedCenterEntities) dataStore;

            if (entities == null)
                return null;

            Setting setting = entities.Settings.FirstOrDefault(s => s.Name == name && s.Version == version);

            return setting == null ? null : setting.Value;
        }

        public static void SetSettingValue(object dataStore, string name, string version, string value)
        {
            FeedCenterEntities entities = (FeedCenterEntities) dataStore;

            if (entities == null)
                return;

            // Try to get the setting from the database that matches the name and version
            Setting setting = entities.Settings.FirstOrDefault(s => s.Name == name && s.Version == version);

            // If there was no setting we need to create it
            if (setting == null)
            {
                // Create the new setting
                setting = new Setting { Name = name, Version = version };

                // Add the setting to the database
                entities.Settings.AddObject(setting);
            }

            // Set the value into the setting
            setting.Value = value;
        }

        public static List<string> GetVersionList(object dataStore)
        {
            FeedCenterEntities entities = (FeedCenterEntities) dataStore;

            if (entities == null)
                return null;

            return (from setting in entities.Settings
                    select setting.Version).Distinct().ToList();
        }

        public static void DeleteSettingsForVersion(object dataStore, string version)
        {
            FeedCenterEntities entities = (FeedCenterEntities) dataStore;

            if (entities == null)
                return;

            // Get all the settings for the current version number
            var settings = entities.Settings.Where(setting => setting.Version == version);

            // Delete each setting
            foreach (Setting setting in settings)
                entities.Settings.DeleteObject(setting);
        }
    }
}
