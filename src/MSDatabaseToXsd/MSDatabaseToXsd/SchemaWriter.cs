using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using static MSDatabaseToXsd.XmlWriterSchemaExtentions;

namespace MSDatabaseToXsd
{
    public static class SchemaWriter
    {
        #region Xml Writer Settings
        private static XmlWriterSettings _xmlSettings = new XmlWriterSettings
        {
            Indent = true,
            NamespaceHandling = NamespaceHandling.OmitDuplicates
        };
        #endregion

        #region Public Methods
        public static async Task WriteSchemaAsync(Schema schema, string filePath, string schemaId, string dataSetName, bool addForeignKeys)
        {
            using (var writer = XmlWriter.Create(filePath, _xmlSettings))
            {
                await writer.WriteStartElementAsync(SchemaNamespacePrefix, "schema", SchemaNamespace);
                await writer.WriteAttributeStringAsync("xmlns", $"http://tempuri.org/{dataSetName}.xsd");
                await writer.WriteXmlNsAsync("mstns", $"http://tempuri.org/{dataSetName}.xsd");
                await writer.WriteAttributeStringAsync("targetNamespace", $"http://tempuri.org/{dataSetName}.xsd");
                await writer.WriteAttributeStringAsync("elementFormDefault", "qualified");
                await writer.WriteAttributeStringAsync("id", schemaId);
                if (addForeignKeys)
                    await writer.WriteXmlNsAsync(MetadataNamespacePrefix, MetadataNamespace);

                await WriteDataSetAsync(writer, dataSetName, schema, addForeignKeys);

                if (addForeignKeys)
                    await WriteForeignKeysAsync(writer, schema.ForeignKeys);

                await writer.WriteEndElementAsync(); // xs:schema
            }
        }

        public static void WriteSchema(Schema schema, string filePath, string schemaId, string dataSetName, bool addForeignKeys)
        {
            WriteSchemaAsync(schema, filePath, schemaId, dataSetName, addForeignKeys).Wait();
        }
        #endregion

        #region Private Methods
        private static async Task WriteDataSetAsync(XmlWriter writer, string dataSetName, Schema schema, bool isMetadataNamespaceDefined)
        {
            await writer.WriteStartSchemaElementAsync("element");
            await writer.WriteAttributeStringAsync("name", dataSetName);

            await writer.WriteStartSchemaElementAsync("complexType");

            foreach (var table in schema.Tables)
                await WriteTableAsync(writer, table);

            await writer.WriteEndElementAsync(); // complextType

            foreach (var primaryKey in schema.PrimaryKeys)
                await WritePrimaryKeyAsync(writer, primaryKey, isMetadataNamespaceDefined);

            await writer.WriteEndElementAsync(); // DataSet element
        }

        private static async Task WriteTableAsync(XmlWriter writer, Table table)
        {
            await writer.WriteStartSchemaElementAsync("element");
            await writer.WriteAttributeStringAsync("name", table.Name);

            await writer.WriteStartSchemaElementAsync("complexType");
            await writer.WriteStartSchemaElementAsync("sequence");

            foreach (var column in table.Columns)
                await WriteColumnAsync(writer, column);

            await writer.WriteEndElementAsync(); // sequence
            await writer.WriteEndElementAsync(); // complexType

            await writer.WriteEndElementAsync(); // element
        }

        private static async Task WriteColumnAsync(XmlWriter writer, Column column)
        {
            await writer.WriteStartSchemaElementAsync("element");
            await writer.WriteAttributeStringAsync("name", column.Name);

            if (!column.Nullable)
                await writer.WriteAttributeStringAsync("minOccurs", "1");

            var schemaDataType = SchemaNamespacePrefix + ":" + ConvertDataType(column.DataType);

            if (column.MaxLength.HasValue && column.MaxLength != -1)
            {
                await writer.WriteStartSchemaElementAsync("simpleType");

                await writer.WriteStartSchemaElementAsync("restriction");
                await writer.WriteAttributeStringAsync("base", schemaDataType);

                await writer.WriteStartSchemaElementAsync("maxLength");
                await writer.WriteAttributeStringAsync("value", column.MaxLength.ToString());

                await writer.WriteEndElementAsync(); // maxLength
                await writer.WriteEndElementAsync(); // restriction
                await writer.WriteEndElementAsync(); // simpleType
            }
            else await writer.WriteAttributeStringAsync("type", schemaDataType);

            await writer.WriteEndElementAsync(); // element
        }

        private static async Task WritePrimaryKeyAsync(XmlWriter writer, PrimaryKey primaryKey, bool isMetadataNamespaceDefined)
        {
            await writer.WriteStartSchemaElementAsync("unique");
            await writer.WriteAttributeStringAsync("name", primaryKey.Name);

            if (isMetadataNamespaceDefined)
                await writer.WriteMedataAttributeAsync("PrimaryKey", "true");

            await writer.WriteStartSchemaElementAsync("selector");
            await writer.WriteAttributeStringAsync("xpath", $".//mstns:{primaryKey.Table}");
            await writer.WriteEndElementAsync();

            await writer.WriteStartSchemaElementAsync("field");
            await writer.WriteAttributeStringAsync("xpath", $"mstns:{primaryKey.Column}");
            await writer.WriteEndElementAsync();

            await writer.WriteEndElementAsync(); // unique
        }

        private static async Task WriteForeignKeysAsync(XmlWriter writer, IEnumerable<ForeignKey> foreignKeys)
        {
            await writer.WriteStartSchemaElementAsync("annotation");
            await writer.WriteStartSchemaElementAsync("appinfo");

            foreach (var foreignKey in foreignKeys)
            {
                await writer.WriteStartMetadataElementAsync("Relationship");
                await writer.WriteAttributeStringAsync("name", foreignKey.Name);
                await writer.WriteMedataAttributeAsync("parent", foreignKey.PrimaryKeyTable);
                await writer.WriteMedataAttributeAsync("child", foreignKey.ForeignKeyTable);
                await writer.WriteMedataAttributeAsync("parentkey", foreignKey.PrimaryKeyColumn);
                await writer.WriteMedataAttributeAsync("childkey", foreignKey.ForeignKeyColumn);

                await writer.WriteEndElementAsync(); // Relationship
            }

            await writer.WriteEndElementAsync(); // annotation
            await writer.WriteEndElementAsync(); // appinfo
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
        #endregion
    }
}
