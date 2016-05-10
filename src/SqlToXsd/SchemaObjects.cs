using System.Collections.Generic;

namespace SqlToXsd
{
    public class Schema
    {
        public Table[] Tables { get; set; }
        public PrimaryKey[] PrimaryKeys { get; set; }
        public ForeignKey[] ForeignKeys { get; set; }
    }

    public class Table
    {
        public string Name { get; set; }
        public Column[] Columns { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public class Column
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public int? MaxLength { get; set; }
        public bool Nullable { get; set; }
        public bool IsIdentity { get; set; }

        public override string ToString()
        {
            return $"{Name}, {DataType}, {Nullable}, {MaxLength}";
        }
    }

    public class PrimaryKey
    {
        public string Name { get; set; }
        public string Table { get; set; }
        public string[] Columns { get; set; }
        //public string Column { get; set; }

        public override string ToString()
        {
            return $"{Name}, {Table}, [{string.Join(",", Columns)}]";
        }
    }

    public class ForeignKey
    {
        public string Name { get; set; }
        public string PrimaryKeyTable { get; set; }
        public string PrimaryKeyColumn { get; set; }
        public string ForeignKeyTable { get; set; }
        public string ForeignKeyColumn { get; set; }
        public string PrimaryTableName { get; set; }
    }
}
