using System;

namespace Jovemnf.MySQL
{
    public class MySQLCloseException : Exception
    {
        public MySQLCloseException()
            : base() { }

        public MySQLCloseException(string message)
            : base(message) { }

        public MySQLCloseException(string format, params object[] args)
            : base(string.Format(format, args)) { }

        public MySQLCloseException(string message, Exception innerException)
            : base(message, innerException) { }

        public MySQLCloseException(string format, Exception innerException, params object[] args)
            : base(string.Format(format, args), innerException) { }
    }
}
