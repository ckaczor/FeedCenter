using System;
using System.Text.RegularExpressions;
using FeedCenter.Options;
using Realms;

namespace FeedCenter
{
    public class FeedItem : RealmObject
    {
        [PrimaryKey]
        [MapTo("ID")]
        public Guid Id { get; set; }

        [MapTo("FeedID")]
        public Guid FeedId { get; set; }

        public string Title { get; set; }
        public string Link { get; set; }
        public string Description { get; set; }
        public bool BeenRead { get; set; }
        public DateTimeOffset LastFound { get; set; }
        public bool New { get; set; }
        public string Guid { get; set; }
        public int Sequence { get; set; }

        public Feed Feed { get; set; }

        public static FeedItem Create()
        {
            return new FeedItem { Id = System.Guid.NewGuid() };
        }

        #region Methods

        public override string ToString()
        {
            var title = Title;

            switch (Properties.Settings.Default.MultipleLineDisplay)
            {
                case Options.MultipleLineDisplay.SingleLine:

                    // Strip any newlines from the title
                    title = Regex.Replace(title, @"\n", " ");

                    break;

                case Options.MultipleLineDisplay.FirstLine:

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

            // Condense multiple spaces to one space
            title = Regex.Replace(title, @"[ ]{2,}", " ");

            // Condense tabs to one space
            title = Regex.Replace(title, @"\t", " ");

            // If the title is blank then put in the "no title" title
            if (title.Length == 0)
                title = Properties.Resources.NoTitleText;

            return title;
        }

        //public void ProcessActions(IEnumerable<FeedAction> feedActions)
        //{
        //    foreach (FeedAction feedAction in feedActions)
        //    {
        //        switch (feedAction.Field)
        //        {
        //            case 1:

        //                Title = Regex.Replace(Title, feedAction.Search, feedAction.Replace);
        //                break;
        //        }
        //    }
        //}

        #endregion
    }
}
