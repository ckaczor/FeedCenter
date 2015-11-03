using System.Data.SqlTypes;

namespace FeedCenter.Data
{
    public static class Extensions
    {
        #region SqlDateTime

        public static SqlDateTime SqlDateTimeZero = new SqlDateTime(0, 0);

        #endregion
    }
}
