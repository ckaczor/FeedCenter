using Realms;

namespace FeedCenter.Options;

public class Setting : RealmObject
{
    [PrimaryKey]
    public string Name { get; set; }
    public string Value { get; set; }

    [Ignored]
    public string Version { get; set; }
}