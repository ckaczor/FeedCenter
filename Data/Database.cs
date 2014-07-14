using Common.Debug;
using FeedCenter.Properties;
using System;
using System.Collections.Generic;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;

namespace FeedCenter.Data
{
    public static class Database
    {
        #region Static database settings

        public static string DatabasePath;

        #endregion

        #region File version

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
                using (var stream = new FileStream(databasePath, FileMode.Open, FileAccess.Read))
                {
                    // Read the file using the binary reader
                    var reader = new BinaryReader(stream);

                    // Seek to the version signature
                    stream.Seek(16, SeekOrigin.Begin);

                    // Read the version signature
                    signature = reader.ReadInt32();

                }
            }
            catch (Exception exception)
            {
                Tracer.WriteException(exception);

                throw;
            }

            // If we know about the version number then return the right enumeration - otherwise unknown
            return versionMapping.ContainsKey(signature) ? versionMapping[signature] : SqlServerCeFileVersion.Unknown;
        }

        #endregion

        public static bool DatabaseExists
        {
            get { return File.Exists(DatabasePath); }
        }

        public static void CreateDatabase()
        {
            Tracer.WriteLine("Creating database engine");

            // Create the database engine
            using (var engine = new SqlCeEngine(string.Format("Data Source={0}", DatabasePath)))
            {
                Tracer.WriteLine("Creating database");

                // Create the database itself
                engine.CreateDatabase();

                Tracer.WriteLine("Running database script");

                // Run the creation script                
                ExecuteScript(Resources.CreateDatabase);
            }
        }

        private static int GetVersion(SqlCeConnection connection)
        {
            string versionString;

            try
            {
                // Check the database version table
                using (var command = new SqlCeCommand("SELECT Value FROM DatabaseVersion", connection))
                    versionString = command.ExecuteScalar().ToString();
            }
            catch (SqlCeException)
            {
                // Check the setting table for the version
                using (var command = new SqlCeCommand("SELECT Value FROM Setting WHERE Name = 'DatabaseVersion'", connection))
                    versionString = command.ExecuteScalar().ToString();
            }

            if (string.IsNullOrEmpty(versionString))
                versionString = "0";

            Tracer.WriteLine("Database version: {0}", versionString);

            return int.Parse(versionString);
        }

        public static void UpdateDatabase()
        {
            Tracer.WriteLine("Getting database file version");

            // Get the database file version
            var fileVersion = GetFileVersion(DatabasePath);

            Tracer.WriteLine("Database file version: {0}", fileVersion);

            // See if we need to upgrade the database file version
            if (fileVersion != SqlServerCeFileVersion.Version40)
            {
                Tracer.WriteLine("Creating database engine");

                // Create the database engine
                using (var engine = new SqlCeEngine(string.Format("Data Source={0}", DatabasePath)))
                {
                    Tracer.WriteLine("Upgrading database");

                    // Upgrade the database (if needed)
                    engine.Upgrade();
                }
            }

            Tracer.WriteLine("Getting database version");

            // Create a database connection
            using (var connection = new SqlCeConnection(string.Format("Data Source={0}", DatabasePath)))
            {
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
                    var version = int.Parse(propertyName.Substring(propertyName.IndexOf("_", StringComparison.Ordinal) + 1));

                    // Add to the script list
                    scriptList[version] = propertyName;
                }

                // Loop over the scripts ordered by version
                foreach (var pair in scriptList.OrderBy(pair => pair.Key))
                {
                    // If the database version is less than or equal to the script version the script needs to run
                    if (databaseVersion <= pair.Key)
                    {
                        // Get the script text
                        var scriptText = Resources.ResourceManager.GetString(pair.Value);

                        // Run the script
                        ExecuteScript(scriptText);
                    }
                }
            }
        }

        public static void MaintainDatabase()
        {
            Tracer.WriteLine("Creating database engine");

            // Create the database engine
            using (var engine = new SqlCeEngine(string.Format("Data Source={0}", DatabasePath)))
            {
                Tracer.WriteLine("Shrinking database");

                // Compact the database
                engine.Shrink();
            }
        }

        private static void ExecuteScript(string scriptText)
        {
            // Create a database connection
            using (var connection = new SqlCeConnection(string.Format("Data Source={0}", DatabasePath)))
            {
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
                    using (var command = new SqlCeCommand(statement, connection))
                        command.ExecuteNonQuery();
                }
            }
        }
    }
}
