using System.Collections.Generic;
using System.Xml;
using static MSDatabaseToXsd.XmlWriterExtensions;

namespace MSDatabaseToXsd
{
    public static class SchemaWriter
    {
        public static void WriteSchema(Schema schema, string filePath, string schemaId, string dataSetName, bool addForeignKeys)
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
                if (addForeignKeys)
                    writer.WriteAttributeString("xmlns", MetadataNamespacePrefix, null, MetadataNamespace);

                WriteDataSet(writer, dataSetName, schema, addForeignKeys);

                writer.WriteEndElement(); // xs:schema
            }
        }

        private static void WriteDataSet(XmlWriter writer, string dataSetName, Schema schema, bool isMetadataNamespaceDefined)
        {
            writer.WriteStartSchemaElement("element");
            writer.WriteAttributeString("name", dataSetName);

            writer.WriteStartSchemaElement("complexType");

            foreach (var table in schema.Tables)
                WriteTable(writer, table);

            writer.WriteEndElement(); // complextType

            foreach (var primaryKey in schema.PrimaryKeys)
                WritePrimaryKey(writer, primaryKey, isMetadataNamespaceDefined);

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

        private static void WritePrimaryKey(XmlWriter writer, PrimaryKey primaryKey, bool isMetadataNamespaceDefined)
        {
            writer.WriteStartSchemaElement("unique");
            writer.WriteAttributeString("name", primaryKey.Name);

            if (isMetadataNamespaceDefined)
                writer.WriteMedataAttribute("PrimaryKey", "true");

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

        private static void WriteForeignKeys(XmlWriter writer, IEnumerable<ForeignKey> foreignKeys)
        {
            writer.WriteStartSchemaElement("appinfo");

            foreach (var foreignKey in foreignKeys)
            {
                writer.WriteStartMetadataElement("Relationship");

                writer.WriteAttributeString("name", foreignKey.Name);
                writer.WriteMedataAttribute("parent", foreignKey.PrimaryKeyTable);
                writer.WriteMedataAttribute("child", foreignKey.ForeignKeyTable);
                writer.WriteMedataAttribute("parentkey", foreignKey.PrimaryKeyColumn);
                writer.WriteMedataAttribute("childkey", foreignKey.ForeignKeyColumn);

                writer.WriteEndElement(); // Relationship
            }
            writer.WriteEndElement(); // appinfo
        }
    }
}
