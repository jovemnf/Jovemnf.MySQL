namespace Jovemnf.MySQL
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    public class MySQLCriteria
    {
        private List<string> list = new List<string>();

        public void AddColumn(string value, string alias = null)
        {
            if (alias != null)
            {
                value = value = value + " as " + alias;
            }
            this.list.Add(value);
        }

        public string Columns
        {
            get
            {
                return string.Join(",", this.list.ToArray());
            }
        }
    }
}

