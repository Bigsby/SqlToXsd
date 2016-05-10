using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using static SqlToXsd.XmlWriterSchemaExtentions;

namespace SqlToXsd
{
    public static class SchemaWriter
    {
        #region Xml Writer Settings
        private static XmlWriterSettings _xmlSettings = new XmlWriterSettings
        {
            Indent = true,
            NamespaceHandling = NamespaceHandling.OmitDuplicates,
            Async = true
        };
        #endregion

        #region Public Methods
        public static async Task WriteSchemaAsync(Schema schema, string filePath, string schemaId, string dataSetName, bool addForeignKeys)
        {
            var @namespace = $"http://tempuri.org/{dataSetName}.xsd";

            using (var writer = XmlWriter.Create(filePath, _xmlSettings))
            {
                await writer.WriteStartElementAsync(SchemaNamespacePrefix, "schema", SchemaNamespace);
                await writer.WriteAttributeStringAsync("xmlns", @namespace);
                await writer.WriteXmlNsAsync("mstns", @namespace);
                await writer.WriteAttributeStringAsync("targetNamespace", @namespace);
                await writer.WriteAttributeStringAsync("elementFormDefault", "qualified");
                await writer.WriteAttributeStringAsync("id", schemaId);
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

            await writer.WriteStartSchemaElementAsync("choice");
            await writer.WriteAttributeStringAsync("minOccurs", "0");
            await writer.WriteAttributeStringAsync("maxOccurs", "unbounded");

            foreach (var table in schema.Tables)
                await WriteTableAsync(writer, table);

            await writer.WriteEndElementAsync(); // choice
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
            await writer.WriteStartSchemaElementAsync("all");

            foreach (var column in table.Columns)
                await WriteColumnAsync(writer, column);

            await writer.WriteEndElementAsync(); // all
            await writer.WriteEndElementAsync(); // complexType

            await writer.WriteEndElementAsync(); // element
        }

        private static async Task WriteColumnAsync(XmlWriter writer, Column column)
        {
            await writer.WriteStartSchemaElementAsync("element");
            await writer.WriteAttributeStringAsync("name", column.Name);

            await writer.WriteAttributeStringAsync("minOccurs", column.Nullable ? "0" : "1");

            await writer.WriteAttributeStringAsync("maxOccurs", "1");

            var schemaDataType = SchemaNamespacePrefix + ":" + ConvertDataType(column.DataType);

            if (column.DataType == "uniqueidentifier")
            {
                await writer.WriteMedataAttributeAsync("DataType", "System.Guid");
                await AddSimpleTypeWithRestriction(writer, schemaDataType, "pattern", "^[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$");
            }
            else if (column.MaxLength.HasValue && column.MaxLength != -1)
                await AddSimpleTypeWithRestriction(writer, schemaDataType, "maxLength", column.MaxLength.ToString());
            else
            {
                await writer.WriteAttributeStringAsync("type", schemaDataType);
                if (column.IsIdentity)
                    await writer.WriteMedataAttributeAsync("AutoIncrement", "true");
            }

            await writer.WriteEndElementAsync(); // element
        }

        private static async Task AddSimpleTypeWithRestriction(XmlWriter writer, string baseType, string restriction, string value)
        {
            await writer.WriteStartSchemaElementAsync("simpleType");

            await writer.WriteStartSchemaElementAsync("restriction");
            await writer.WriteAttributeStringAsync("base", baseType);

            await writer.WriteStartSchemaElementAsync(restriction);
            await writer.WriteAttributeStringAsync("value", value);

            await writer.WriteEndElementAsync(); // restriction element
            await writer.WriteEndElementAsync(); // restriction
            await writer.WriteEndElementAsync(); // simpleType
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

            foreach (var column in primaryKey.Columns)
            {
                await writer.WriteStartSchemaElementAsync("field");
                await writer.WriteAttributeStringAsync("xpath", $"mstns:{column}");
                await writer.WriteEndElementAsync();
            }

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

                case "date":
                case "timestamp":
                case "smalldatetime":
                case "datetime":
                case "time":
                    return "dateTime";

                case "smallmoney":
                case "numeric":
                case "money":
                case "decimal":
                    return "decimal";

                case "varbinary":
                case "image":
                case "binary":
                    return "base64Binary";

                default:
                    //case "xml":
                    //case "uniqueidentifier":
                    //case "varchar":
                    //case "text":
                    //case "sysname":
                    //case "sql_variant":
                    //case "ntext":
                    //case "nchar":
                    //case "char":
                    //case "nvarchar":
                    return "string";
            }
        }
        #endregion
    }
}