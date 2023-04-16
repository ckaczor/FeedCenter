using System.IO;

namespace FeedCenter.Data;

public static class Database
{
    public static string DatabaseFile { get; set; }
    public static string DatabasePath { get; set; }

    public static FeedCenterEntities Entities { get; set; }

    public static bool Exists => File.Exists(DatabaseFile);

    public static bool Loaded { get; set; }

    public static void Load()
    {
        if (Loaded) return;

        Entities = new FeedCenterEntities();

        Loaded = true;
    }
}