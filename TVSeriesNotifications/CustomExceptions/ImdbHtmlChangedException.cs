using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVSeriesNotifications.CustomExceptions
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
