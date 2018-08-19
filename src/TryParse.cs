namespace Jovemnf.MySQL
{
    using System;

    internal class TryParse
    {
        public static bool ToBoolean(object value)
        {
            int option = ToInt32(value);
            try
            {
                return Convert.ToBoolean(option);
            }
            catch
            {
                return false;
            }
        }

        public static decimal ToDecimal(object value)
        {
            try
            {
                return Convert.ToDecimal(value);
            }
            catch
            {
                return 0M;
            }
        }

        public static double ToDouble(object value)
        {
            try
            {
                return Convert.ToDouble(value);
            }
            catch
            {
                return 0.00;
            }
        }

        public static long ToLong(object value)
        {
            try
            {
                return Convert.ToInt64(value);
            }
            catch
            {
                return 0;
            }
        }

        public static int ToInt32(object value)
        {
            try
            {
                return int.Parse(value.ToString());
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }
}

