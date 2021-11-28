namespace TVSeriesNotifications.Infrastructure.Adapters.HtmlParser.Exceptions
{
    public class ImdbHtmlChangedException : Exception
    {
        public ImdbHtmlChangedException(string message)
            : base(message)
        {
        }

        public ImdbHtmlChangedException(string message, Exception ex)
            : base(message, ex)
        {
        }
    }
}
