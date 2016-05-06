using System.Collections.Generic;

namespace MSDatabaseToXsd
{
    public class Schema
    {
        public IEnumerable<Table> Tables { get; set; }
        public IEnumerable<PrimaryKey> PrimaryKeys { get; set; }
    }

    public class Table
    {
        public string Name { get; set; }
        public IEnumerable<Column> Columns { get; set; }

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

        public override string ToString()
        {
            return $"{Name}, {DataType}, {Nullable}, {MaxLength}";
        }
    }

    public class PrimaryKey
    {
        public string Name { get; set; }
        public string Table { get; set; }
        public string Column { get; set; }

        public override string ToString()
        {
            return $"{Name}, {Table}, {Column}";
        }
    }
}
