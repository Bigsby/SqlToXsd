using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Xml;
using static System.Console;
using static MSDatabaseToXsd.XmlWriterExtensions;

namespace MSDatabaseToXsd
{
    class Program
    {
        private static string[] _helpParameters = new[]
{
            "-h",
            "help",
            "-help",
            "\\help"
        };

        static void Main(string[] args)
        {
            if (args.Length == 1 && _helpParameters.Contains(args[0]))
            {
                ShowUsage();
                return;
            }

            if (args.Length != 4)
                ShowParametersError();

            var connectionString = args[0];
            var schameId = args[1];
            var dataSetName = args[2];
            var targetFile = args[3];

            try
            {
                WriteLine("Reading schema from database...");

                var schema = GetSchema(connectionString);

                WriteLine($"{schema.Tables.Count()} tables found.");

                WriteLine("Writing schema to file...");

                WriteSchema(schema, targetFile, schameId, dataSetName);

                WriteLine("Done!");
            }
            catch (Exception ex)
            {
                Error.WriteLine("Error!!!");
                Error.WriteLine(ex.Message);
                Error.WriteLine(ex.StackTrace);
            }
        }

        private static void ShowParametersError()
        {
            Error.WriteLine("Invalid Paramters!!!");
            ShowUsage();
            Environment.Exit(1);
        }

        private static void ShowUsage()
        {
            WriteLine("Usage:");
            WriteLine("\tDatabaseToXsd connectionstring schemaId dataSetName targetFile");
            WriteLine("Example:");
            WriteLine("\tDatabaseToXsd server=.\\sqexpress;database=OneviewManagement;uid=sa;pwd=password OmeviewManagementSchema OneviewManagementDataSet OneviewManagementSchema.xsd");
        }

        private static Schema GetSchema(string connectionString)
        {
            var tables = new List<Table>();
            var primaryKeys = new List<PrimaryKey>();

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                using (var tableCommand = new SqlCommand("SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'", conn))
                using (var tableReader = tableCommand.ExecuteReader())
                    while (tableReader.Read())
                        tables.Add(new Table
                        {
                            Name = tableReader.GetString(2)
                        });

                foreach (var table in tables)
                {
                    using (var columnCommand = new SqlCommand($"SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{table.Name}'", conn))
                    using (var columnReader = columnCommand.ExecuteReader())
                    {
                        var columns = new List<Column>();
                        while (columnReader.Read())
                            columns.Add(new Column
                            {
                                Name = columnReader.GetString(3),
                                DataType = columnReader.GetString(7),
                                Nullable = columnReader.GetString(6) == "NO" ? false : true,
                                MaxLength = columnReader.IsDBNull(8) ? (int?)null : columnReader.GetInt32(8)
                            });
                        table.Columns = columns;
                    }

                    using (var primaryKeyCommand = new SqlCommand($"sp_pkeys '{table.Name}'", conn))
                    using (var primaryKeyReader = primaryKeyCommand.ExecuteReader())
                        while (primaryKeyReader.Read())
                            primaryKeys.Add(new PrimaryKey
                            {
                                Table = table.Name,
                                Name = primaryKeyReader.GetString(5),
                                Column = primaryKeyReader.GetString(3)
                            });
                }

                conn.Close();
            }

            return new Schema
            {
                Tables = tables,
                PrimaryKeys = primaryKeys
            };
        }

        private static void WriteSchema(Schema schema, string filePath, string schemaId, string dataSetName)
        {
            using (var writer = XmlWriter.Create(filePath, new XmlWriterSettings
            {
                Indent = true,
                NamespaceHandling = NamespaceHandling.OmitDuplicates
            }))
            {
                writer.WriteStartElement(SchemaNamespacePrefix, "schema", SchemaNamespace);
                writer.WriteAttributeString("xmlns", $"http://tempuri.org/{dataSetName}.xsd");
                writer.WriteAttributeString("xmlns", "mstns", null, $"http://tempuri.org/{dataSetName}.xsd");
                writer.WriteAttributeString("targetNamespace", $"http://tempuri.org/{dataSetName}.xsd");
                writer.WriteAttributeString("elementFormDefault", "qualified");
                writer.WriteAttributeString("id", schemaId);

                WriteDataSet(writer, dataSetName, schema);

                writer.WriteEndElement(); // xs:schema
            }
        }

        private static void WriteDataSet(XmlWriter writer, string dataSetName, Schema schema)
        {
            writer.WriteStartSchemaElement("element");
            writer.WriteAttributeString("name", dataSetName);

            writer.WriteStartSchemaElement("complexType");

            foreach (var table in schema.Tables)
                WriteTable(writer, table);

            writer.WriteEndElement(); // complextType

            foreach (var primaryKey in schema.PrimaryKeys)
                WritePrimaryKey(writer, primaryKey);

            writer.WriteEndElement(); // DataSet element
        }

        private static void WriteTable(XmlWriter writer, Table table)
        {
            writer.WriteStartSchemaElement("element");
            writer.WriteAttributeString("name", table.Name);

            writer.WriteStartSchemaElement("complexType");
            writer.WriteStartSchemaElement("sequence");

            foreach (var column in table.Columns)
                WriteColumn(writer, column);

            writer.WriteEndElement(); // sequence
            writer.WriteEndElement(); // complexType

            writer.WriteEndElement(); // element
        }

        private static void WriteColumn(XmlWriter writer, Column column)
        {
            writer.WriteStartSchemaElement("element");
            writer.WriteAttributeString("name", column.Name);

            if (!column.Nullable)
                writer.WriteAttributeString("minOccurs", "1");

            var schemaDataType = SchemaNamespacePrefix + ":" + ConvertDataType(column.DataType);

            if (column.MaxLength.HasValue && column.MaxLength != -1)
            {
                writer.WriteStartSchemaElement("simpleType");

                writer.WriteStartSchemaElement("restriction");
                writer.WriteAttributeString("base", schemaDataType);

                writer.WriteStartSchemaElement("maxLength");
                writer.WriteAttributeString("value", column.MaxLength.ToString());

                writer.WriteEndElement(); // maxLength
                writer.WriteEndElement(); // restriction
                writer.WriteEndElement(); // simpleType
            }
            else writer.WriteAttributeString("type", schemaDataType);

            writer.WriteEndElement(); // element
        }

        private static void WritePrimaryKey(XmlWriter writer, PrimaryKey primaryKey)
        {
            writer.WriteStartSchemaElement("unique");
            writer.WriteAttributeString("name", primaryKey.Name);

            writer.WriteStartSchemaElement("selector");
            writer.WriteAttributeString("xpath", $".//mstns:{primaryKey.Table}");
            writer.WriteEndElement();

            writer.WriteStartSchemaElement("field");
            writer.WriteAttributeString("xpath", $"mstns:{primaryKey.Column}");
            writer.WriteEndElement();

            writer.WriteEndElement(); // unique
        }

        private static string ConvertDataType(string dbType)
        {
            switch (dbType)
            {
                case "bit": return "boolean";
                case "bigint": return "long";
                case "float": return "double";
                case "int": return "int";
                case "real": return "float";
                case "smallint": return "short";
                case "tinyint": return "unsignedByte";

                case "timestamp":
                case "smalldatetime":
                case "datetime":
                    return "datetime";

                case "xml":
                case "uniqueidentifier":
                case "varchar":
                case "text":
                case "sysname":
                case "sql_variant":
                case "ntext":
                case "nchar":
                case "char":
                case "nvarchar":
                    return "string";

                case "smallmoney":
                case "numeric":
                case "money":
                case "decimal":
                    return "decimal";

                case "varbinary":
                case "image":
                case "binary":
                    return "base64Binary";

                default: return "unknown";
            }
        }

        private static void ReadSchema(SqlConnection conn, StreamWriter writer, string name)
        {
            var table = name == "Schema" ? conn.GetSchema() : conn.GetSchema(name);

            writer.WriteLine(name + ":");

            foreach (DataColumn column in table.Columns)
                writer.WriteLine(string.Format("{0}:{1}", column.ColumnName, column.DataType));

            foreach (DataRow row in table.Rows)
                writer.WriteLine(string.Join(",", row.ItemArray));

            writer.WriteLine();
        }
    }
}
