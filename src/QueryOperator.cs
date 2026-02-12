using System;
using System.ComponentModel;

namespace Jovemnf.MySQL.Builder
{
    public enum QueryOperator
    {
        [Description("=")]
        Equals,
        [Description("<>")]
        NotEquals,
        [Description("<")]
        LessThan,
        [Description("<=")]
        LessThanOrEqual,
        [Description(">")]
        GreaterThan,
        [Description(">=")]
        GreaterThanOrEqual,
        [Description("LIKE")]
        Like,
        [Description("NOT LIKE")]
        NotLike,
        [Description("IS NULL")]
        IsNull,
        [Description("IS NOT NULL")]
        IsNotNull,
        [Description("IN")]
        In,
        [Description("NOT IN")]
        NotIn,
        [Description("BETWEEN")]
        Between,
        [Description("REGEXP")]
        Regexp,
        [Description("NOT REGEXP")]
        NotRegexp
    }

    public static class QueryOperatorExtensions
    {
        public static string ToSqlString(this QueryOperator op)
        {
            var fieldInfo = op.GetType().GetField(op.ToString());
            var attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return attributes.Length > 0 ? attributes[0].Description : op.ToString();
        }
    }
}
