using System.IO;

namespace FeedCenter.Data;

public static class Database
{
    public static string DatabaseFile { get; set; }
    public static string DatabasePath { get; set; }

    public static bool Exists => File.Exists(DatabaseFile);
}