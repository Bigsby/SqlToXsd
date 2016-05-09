using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace SqlToXsd
{
    public static class SchemaReader
    {
        public static Schema ReadSchema(string connectionString, bool addForeignKeys)
        {
            return ReadSchemaAsync(connectionString, addForeignKeys).Result;
        }

        public static async Task<Schema> ReadSchemaAsync(string connectionString, bool addForeignKeys)
        {
            var tables = new List<Table>();
            var primaryKeys = new List<PrimaryKey>();
            var foreignKeys = new List<ForeignKey>();

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                await CallDatabase(conn, "SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'", reader =>
                    tables.Add(new Table
                    {
                        Name = reader.GetString(2)
                    })
                ); ;

                foreach (var table in tables)
                {
                    var columns = new List<Column>();
                    await CallDatabase(conn, $"SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{table.Name}'", reader =>
                        columns.Add(new Column
                        {
                            Name = reader.GetString(3),
                            DataType = reader.GetString(7),
                            Nullable = reader.GetString(6) == "NO" ? false : true,
                            MaxLength = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8)
                        })
                    );
                    table.Columns = columns.ToArray();

                    await CallDatabase(conn, $"sp_pkeys '{table.Name}'", reader => AddPrimaryKey(primaryKeys, table, reader));

                    if (addForeignKeys)
                        await CallDatabase(conn, $"sp_fkeys '{table.Name}'", reader =>
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
                Tables = tables.ToArray(),
                PrimaryKeys = primaryKeys.ToArray(),
                ForeignKeys = foreignKeys.ToArray()
            };
        }

        private static void AddPrimaryKey(IList<PrimaryKey> primaryKeys, Table table, SqlDataReader reader)
        {
            var pkName = reader.GetString(5);
            var columNName = reader.GetString(3);

            var existing = primaryKeys.FirstOrDefault(pk => pk.Name == pkName);

            if (null == existing)
                primaryKeys.Add(new PrimaryKey
                {
                    Table = table.Name,
                    Name = pkName,
                    Columns = new[] { columNName }
                });
            else
                existing.Columns = existing.Columns.Union(new[] { columNName }).ToArray();
        }

        private static async Task CallDatabase(SqlConnection conn, string commandString, Action<SqlDataReader> process)
        {
            using (var command = new SqlCommand(commandString, conn))
            using (var reader = command.ExecuteReader())
                while (await reader.ReadAsync())
                    process(reader);
        }
    }
}
