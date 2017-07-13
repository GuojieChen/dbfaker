using System.Collections.Generic;

namespace dbfaker
{
    public class DbTable
    {
        public string Name { get; set; }

        public string SechemaName { get; set; }

        public IEnumerable<DbColumn> Columns { get; set; }

        public DbIndex PrimaryKeys { get; set; }

        public IEnumerable<DbIndex> IndexKeys { get; set; }

        public DbTable()
        {
            Columns = new List<DbColumn>();
        }
    }

    public class DbColumn
    {
        public string SchemaName { get; set; }

        public string TableName { get; set; }

        public string ColumnName { get; set; }

        public string DataType { get; set; }

        public string DefaultValue { get; set; }

        public bool IsIdentity { get; set; }

        public bool IsNullable { get; set; }

        public int MaxLength { get; set; }

        public int OrdinalPosition { get; set; }
    }

    public class DbIndex
    {
        //http://stackoverflow.com/questions/765867/list-of-all-index-index-columns-in-sql-server-db

        public string TableName { get; set; }

        public string IndexName { get; set; }

        public string ColumnName { get; set; }

        public bool IsUnique { get; set; }

        public bool IsDescending { get; set; }

        public bool IsIncludedColumn { get; set; }
    }
}
