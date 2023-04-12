using Dapper;
using FeedCenter.Options;
using FeedCenter.Properties;
using Realms;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;

namespace FeedCenter.Data;

public static class LegacyDatabase
{
    public static string DatabaseFile { get; set; }
    public static string DatabasePath { get; set; }

    private enum SqlServerCeFileVersion
    {
        Unknown,
        Version20,
        Version30,
        Version35,
        Version40,
    }

    private static SqlServerCeFileVersion GetFileVersion(string databasePath)
    {
        // Create a mapping of version numbers to the version enumeration
        var versionMapping = new Dictionary<int, SqlServerCeFileVersion>
        {
            { 0x73616261, SqlServerCeFileVersion.Version20 },
            { 0x002dd714, SqlServerCeFileVersion.Version30 },
            { 0x00357b9d, SqlServerCeFileVersion.Version35 },
            { 0x003d0900, SqlServerCeFileVersion.Version40 }
        };

        int signature;

        try
        {
            // Open the database file
            using var stream = new FileStream(databasePath, FileMode.Open, FileAccess.Read);
            // Read the file using the binary reader
            var reader = new BinaryReader(stream);

            // Seek to the version signature
            stream.Seek(16, SeekOrigin.Begin);

            // Read the version signature
            signature = reader.ReadInt32();
        }
        catch (Exception exception)
        {
            Log.Logger.Error(exception, "Exception");

            throw;
        }

        // If we know about the version number then return the right enumeration - otherwise unknown
        return versionMapping.TryGetValue(signature, out var value) ? value : SqlServerCeFileVersion.Unknown;
    }

    public static bool Exists => File.Exists(DatabaseFile);

    private static int GetVersion(SqlCeConnection connection)
    {
        string versionString;

        try
        {
            // Check the database version table
            using var command = new SqlCeCommand("SELECT Value FROM DatabaseVersion", connection);
            versionString = command.ExecuteScalar().ToString();
        }
        catch (SqlCeException)
        {
            // Check the setting table for the version
            using var command = new SqlCeCommand("SELECT Value FROM Setting WHERE Name = 'DatabaseVersion'", connection);
            versionString = command.ExecuteScalar().ToString();
        }

        if (string.IsNullOrEmpty(versionString))
            versionString = "0";

        Log.Logger.Information("Database version: {0}", versionString);

        return int.Parse(versionString);
    }

    public static void UpdateDatabase()
    {
        Log.Logger.Information("Getting database file version");

        // Get the database file version
        var fileVersion = GetFileVersion(DatabaseFile);

        Log.Logger.Information("Database file version: {0}", fileVersion);

        // See if we need to upgrade the database file version
        if (fileVersion != SqlServerCeFileVersion.Version40)
        {
            Log.Logger.Information("Creating database engine");

            // Create the database engine
            using var engine = new SqlCeEngine($"Data Source={DatabaseFile}");
            Log.Logger.Information("Upgrading database");

            // Upgrade the database (if needed)
            engine.Upgrade();
        }

        Log.Logger.Information("Getting database version");

        // Create a database connection
        using var connection = new SqlCeConnection($"Data Source={DatabaseFile}");
        // Open the connection
        connection.Open();

        // Get the database version
        var databaseVersion = GetVersion(connection);

        // Create a dictionary of database upgrade scripts and their version numbers
        var scriptList = new Dictionary<int, string>();

        // Loop over the properties of the resource object looking for update scripts
        foreach (var property in typeof(Resources).GetProperties().Where(property => property.Name.StartsWith("DatabaseUpdate")))
        {
            // Get the name of the property
            var propertyName = property.Name;

            // Extract the version from the name
            var version = int.Parse(propertyName[(propertyName.IndexOf("_", StringComparison.Ordinal) + 1)..]);

            // Add to the script list
            scriptList[version] = propertyName;
        }

        // Loop over the scripts ordered by version
        foreach (var pair in scriptList.OrderBy(pair => pair.Key))
        {
            // If the database version is beyond this script then we can skip it
            if (databaseVersion > pair.Key) continue;

            // Get the script text
            var scriptText = Resources.ResourceManager.GetString(pair.Value);

            // Run the script
            ExecuteScript(scriptText);
        }
    }

    public static void MaintainDatabase()
    {
        Log.Logger.Information("Creating database engine");

        // Create the database engine
        using var engine = new SqlCeEngine($"Data Source={DatabaseFile}");
        Log.Logger.Information("Shrinking database");

        // Compact the database
        engine.Shrink();
    }

    public static void MigrateDatabase()
    {
        var realmConfiguration = new RealmConfiguration($"{Database.DatabaseFile}");

        var realm = Realm.GetInstance(realmConfiguration);

        if (!File.Exists(DatabaseFile))
            return;

        using var connection = new SqlCeConnection($"Data Source={DatabaseFile}");

        connection.Open();

        var settings = connection.Query<Setting>("SELECT * FROM Setting").OrderBy(s => s.Version).ToList();
        var categories = connection.Query<Category>("SELECT * FROM Category").ToList();
        var feeds = connection.Query<Feed>("SELECT * FROM Feed").ToList();
        var feedItems = connection.Query<FeedItem>("SELECT * FROM FeedItem").ToList();

        realm.Write(() =>
        {
            foreach (var category in categories)
            {
                if (category.Name == Category.DefaultName)
                    category.IsDefault = true;

                category.Feeds = feeds.Where(f => f.CategoryId == category.Id).ToList();
            }

            foreach (var feed in feeds)
            {
                feed.CategoryId = categories.First(c => c.Id == feed.CategoryId).Id;
            }

            foreach (var feedItem in feedItems)
            {
                var feed = feeds.First(f => f.Id == feedItem.FeedId);

                feed.Items.Add(feedItem);
            }

            realm.Add(feeds);
            realm.Add(categories);
            realm.Add(settings, true);
        });

        connection.Close();

        File.Move(DatabaseFile, DatabaseFile + "_bak");
    }

    private static void ExecuteScript(string scriptText)
    {
        // Create a database connection
        using var connection = new SqlCeConnection($"Data Source={DatabaseFile}");
        // Open the connection
        connection.Open();

        // Setup the delimiters
        var delimiters = new[] { "\r\nGO\r\n" };

        // Split the script at the delimiters
        var statements = scriptText.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

        // Loop over each statement in the script
        foreach (var statement in statements)
        {
            // Execute the statement
            using var command = new SqlCeCommand(statement, connection);
            command.ExecuteNonQuery();
        }
    }
}