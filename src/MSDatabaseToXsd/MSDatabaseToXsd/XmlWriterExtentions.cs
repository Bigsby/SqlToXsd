using System.Xml;

namespace MSDatabaseToXsd
{
    public static class XmlWriterExtensions
    {
        public const string SchemaNamespace = "http://www.w3.org/2001/XMLSchema";
        public const string SchemaNamespacePrefix = "xs";
        public const string MetadataNamespace = "urn:schemas-microsoft-com:xml-msdata";
        public const string MetadataNamespacePrefix = "msdata";

        public static void WriteStartSchemaElement(this XmlWriter writer, string name)
        {
            writer.WriteStartElement(name, SchemaNamespace);
        }

        public static void WriteStartMetadataElement(this XmlWriter writer, string name)
        {
            writer.WriteStartElement(name, MetadataNamespace);
        }

        public static void WriteMedataAttribute(this XmlWriter writer, string name, string value)
        {
            writer.WriteAttributeString(MetadataNamespacePrefix, name, null, value);
        }
    }
}
