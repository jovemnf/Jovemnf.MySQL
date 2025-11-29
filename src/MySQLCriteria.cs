namespace Jovemnf.MySQL
{
    using System;
    using System.Collections.Generic;

    public class MySQLCriteria
    {
        private List<string> list = new List<string>();

        public void AddColumn(string value, string alias = null)
        {
            if (alias != null)
            {
                value = $"{value} as {alias}";
            }
            this.list.Add(value);
        }

        public string Columns
        {
            get
            {
                return string.Join(",", this.list);
            }
        }
    }
}

