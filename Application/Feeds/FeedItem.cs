using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FeedCenter
{
    public partial class FeedItem
    {
        public static FeedItem Create()
        {
            return new FeedItem { ID = System.Guid.NewGuid() };
        }

        #region Methods

        public override string ToString()
        {
            string title = Title;

            switch (Properties.Settings.Default.MultipleLineDisplay)
            {
                case Options.MultipleLineDisplay.SingleLine:

                    // Strip any newlines from the title
                    title = Regex.Replace(title, @"\n", " ");

                    break;

                case Options.MultipleLineDisplay.FirstLine:

                    // Find the first newline
                    int newlineIndex = title.IndexOf("\n", StringComparison.Ordinal);

                    // If a newline was found return everything before it
                    if (newlineIndex > -1)
                        title = title.Substring(0, newlineIndex);

                    break;
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

        public void ProcessActions(IEnumerable<FeedAction> feedActions)
        {
            foreach (FeedAction feedAction in feedActions)
            {
                switch (feedAction.Field)
                {
                    case 1:

                        Title = Regex.Replace(Title, feedAction.Search, feedAction.Replace);
                        break;
                }
            }
        }

        #endregion
    }
}
