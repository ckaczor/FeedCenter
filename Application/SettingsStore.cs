using System;
using System.Collections.Generic;
using System.Linq;

namespace FeedCenter
{
    public static class SettingsStore
    {
        public static object OpenDataStore()
        {
            var entities = new FeedCenterEntities();

            return entities.Database.Exists() ? entities : null;
        }

        public static void CloseDataStore(object dataStore)
        {
            var entities = (FeedCenterEntities) dataStore;

            if (entities == null)
                return;

            entities.SaveChanges();

            entities.Dispose();
        }

        public static string GetSettingValue(object dataStore, string name, Version version)
        {
            var entities = (FeedCenterEntities) dataStore;

            if (entities == null)
                return null;

            var versionString = version.ToString();

            var setting = entities.Settings.FirstOrDefault(s => s.Name == name && s.Version == versionString);

            return setting?.Value;
        }

        public static void SetSettingValue(object dataStore, string name, Version version, string value)
        {
            var entities = (FeedCenterEntities) dataStore;

            if (entities == null)
                return;

            var versionString = version.ToString();

            // Try to get the setting from the database that matches the name and version
            var setting = entities.Settings.FirstOrDefault(s => s.Name == name && s.Version == versionString);

            // If there was no setting we need to create it
            if (setting == null)
            {
                // Create the new setting
                setting = new Setting { Name = name, Version = version.ToString() };

                // Add the setting to the database
                entities.Settings.Add(setting);
            }

            // Set the value into the setting
            setting.Value = value;
        }

        public static List<Version> GetVersionList(object dataStore)
        {
            var entities = (FeedCenterEntities) dataStore;

            // Get a distinct list of version strings
            var versions = entities?.Settings.Select(s => s.Version).Distinct().ToList();

            // Create a version object for each string and return the list
            return versions?.Select(s => new Version(s)).ToList();
        }

        public static void DeleteSettingsForVersion(object dataStore, Version version)
        {
            var entities = (FeedCenterEntities) dataStore;

            if (entities == null)
                return;

            var versionString = version.ToString();

            // Get all the settings for the current version number
            var settings = entities.Settings.Where(setting => setting.Version == versionString);

            // Delete each setting
            foreach (var setting in settings)
                entities.Settings.Remove(setting);
        }
    }
}
