using FeedCenter.Data;
using FeedCenter.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FeedCenter
{
    public static class SettingsStore
    {
        public static object OpenDataStore()
        {
            return Database.Entities;
        }

        public static void CloseDataStore(object dataStore)
        {
        }

        public static string GetSettingValue(object dataStore, string name, Version version)
        {
            var entities = (FeedCenterEntities) dataStore;

            var setting = entities?.Settings.FirstOrDefault(s => s.Name == name);

            return setting?.Value;
        }

        public static void SetSettingValue(object dataStore, string name, Version version, string value)
        {
            var entities = (FeedCenterEntities) dataStore;

            if (entities == null)
                return;

            // Try to get the setting from the database that matches the name and version
            var setting = entities.Settings.FirstOrDefault(s => s.Name == name);

            entities.SaveChanges(() =>
            {
                // If there was no setting we need to create it
                if (setting == null)
                {
                    // Create the new setting
                    setting = new Setting { Name = name };

                    // Add the setting to the database
                    entities.Settings.Add(setting);
                }

                // Set the value into the setting
                setting.Value = value;
            });
        }

        public static List<Version> GetVersionList(object dataStore) => null;

        public static void DeleteSettingsForVersion(object dataStore, Version version)
        {
        }
    }
}
