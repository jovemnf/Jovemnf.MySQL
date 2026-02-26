namespace Jovemnf.MySQL
{
    using System;
    using System.Globalization;

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
                return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
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
                return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0.00;
            }
        }

        public static float ToSingle(object value)
        {
            try
            {
                return Convert.ToSingle(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0f;
            }
        }

        public static short ToInt16(object value)
        {
            try
            {
                return Convert.ToInt16(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0;
            }
        }

        public static byte ToByte(object value)
        {
            try
            {
                return Convert.ToByte(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0;
            }
        }

        public static Guid ToGuid(object value)
        {
            if (value == null || value == DBNull.Value)
                return Guid.Empty;

            if (value is Guid guid)
                return guid;

            if (value is string str && Guid.TryParse(str, out Guid g1))
                return g1;

            if (value is byte[] bytes && bytes.Length == 16)
                return new Guid(bytes);

            return Guid.Empty;
        }

        public static object ChangeType(object value, Type targetType)
        {
            if (value == null || value == DBNull.Value)
                return null;

            if (targetType == typeof(bool)) return ToBoolean(value);
            if (targetType == typeof(int)) return ToInt32(value);
            if (targetType == typeof(long)) return ToLong(value);
            if (targetType == typeof(float)) return ToSingle(value);
            if (targetType == typeof(double)) return ToDouble(value);
            if (targetType == typeof(decimal)) return ToDecimal(value);
            if (targetType == typeof(short)) return ToInt16(value);
            if (targetType == typeof(byte)) return ToByte(value);
            if (targetType == typeof(Guid)) return ToGuid(value);
            if (targetType == typeof(string)) return value.ToString();
            if (targetType == typeof(DateTime)) return Convert.ToDateTime(value);

            try
            {
                return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }

        public static long ToLong(object value)
        {
            try
            {
                return Convert.ToInt64(value, CultureInfo.InvariantCulture);
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
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }
}

