using System;
using System.Text.RegularExpressions;
using FeedCenter.Options;
using Realms;

namespace FeedCenter;

public partial class FeedItem : RealmObject
{
    public bool BeenRead { get; set; }
    public string Description { get; set; }

    public Feed Feed { get; set; }

    public Guid FeedId { get; set; }
    public string Guid { get; set; }

    [PrimaryKey]
    public Guid Id { get; set; }

    public DateTimeOffset LastFound { get; set; }
    public string Link { get; set; }
    public bool New { get; set; }
    public int Sequence { get; set; }

    public string Title { get; set; }

    public static FeedItem Create()
    {
        return new FeedItem { Id = System.Guid.NewGuid() };
    }

    public override string ToString()
    {
        var title = Title;

        switch (Properties.Settings.Default.MultipleLineDisplay)
        {
            case MultipleLineDisplay.SingleLine:

                // Strip any newlines from the title
                title = NewlineRegex().Replace(title, " ");

                break;

            case MultipleLineDisplay.FirstLine:

                // Find the first newline
                var newlineIndex = title.IndexOf("\n", StringComparison.Ordinal);

                // If a newline was found return everything before it
                if (newlineIndex > -1)
                    title = title[..newlineIndex];

                break;
            case MultipleLineDisplay.Normal:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        title ??= string.Empty;

        // Condense multiple spaces to one space
        title = MultipleSpaceRegex().Replace(title, " ");

        // Condense tabs to one space
        title = TabRegex().Replace(title, " ");

        // If the title is blank then put in the "no title" title
        if (title.Length == 0)
            title = Properties.Resources.NoTitleText;

        return title;
    }

    [GeneratedRegex("\\n")]
    private static partial Regex NewlineRegex();

    [GeneratedRegex("[ ]{2,}")]
    private static partial Regex MultipleSpaceRegex();

    [GeneratedRegex("\\t")]
    private static partial Regex TabRegex();
}