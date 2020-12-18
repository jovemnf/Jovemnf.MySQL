using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jovemnf.MySQL
{
    public class MySQLConnectException : Exception
    {
        public MySQLConnectException()
            : base() { }

        public MySQLConnectException(string message)
            : base(message) { }

        public MySQLConnectException(string format, params object[] args)
            : base(string.Format(format, args)) { }

        public MySQLConnectException(string message, Exception innerException)
            : base(message, innerException) { }

        public MySQLConnectException(string format, Exception innerException, params object[] args)
            : base(string.Format(format, args), innerException) { }
    }
}