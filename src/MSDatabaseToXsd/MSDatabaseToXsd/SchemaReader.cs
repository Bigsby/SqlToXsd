using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace MSDatabaseToXsd
{
    public static class SchemaReader
    {
        public static Schema ReadSchema(string connectionString, bool addForeignKeys)
        {
            var tables = new List<Table>();
            var primaryKeys = new List<PrimaryKey>();
            var foreignKeys = new List<ForeignKey>();

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                CallDatabase(conn, "SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'", reader =>
                    tables.Add(new Table
                    {
                        Name = reader.GetString(2)
                    })
                ); ;

                foreach (var table in tables)
                {
                    var columns = new List<Column>();
                    CallDatabase(conn, $"SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{table.Name}'", reader =>
                        columns.Add(new Column
                        {
                            Name = reader.GetString(3),
                            DataType = reader.GetString(7),
                            Nullable = reader.GetString(6) == "NO" ? false : true,
                            MaxLength = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8)
                        })
                    );
                    table.Columns = columns;

                    CallDatabase(conn, $"sp_pkeys '{table.Name}'", reader =>
                        primaryKeys.Add(new PrimaryKey
                        {
                            Table = table.Name,
                            Name = reader.GetString(5),
                            Column = reader.GetString(3)
                        })
                    );

                    if (addForeignKeys)
                        CallDatabase(conn, $"sp_fkeys '{table.Name}'", reader =>
                            foreignKeys.Add(new ForeignKey
                            {
                                PrimaryKeyTable = reader.GetString(2),
                                PrimaryKeyColumn = reader.GetString(3),
                                ForeignKeyTable = reader.GetString(6),
                                ForeignKeyColumn = reader.GetString(7),
                                Name = reader.GetString(11),
                                PrimaryTableName = reader.GetString(12)
                            })
                        );
                }

                conn.Close();
            }

            return new Schema
            {
                Tables = tables,
                PrimaryKeys = primaryKeys,
                ForeignKeys = foreignKeys
            };
        }
            
        private static void CallDatabase(SqlConnection conn, string commandString, Action<SqlDataReader> process)
        {
            using (var command = new SqlCommand(commandString, conn))
            using (var reader = command.ExecuteReader())
                while (reader.Read())
                    process(reader);
        }
    }
}
