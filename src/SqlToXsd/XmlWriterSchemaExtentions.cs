using System.Threading.Tasks;
using System.Xml;

namespace SqlToXsd
{
    public static class XmlWriterSchemaExtentions
    {
        public const string SchemaNamespace = "http://www.w3.org/2001/XMLSchema";
        public const string SchemaNamespacePrefix = "xs";
        public const string MetadataNamespace = "urn:schemas-microsoft-com:xml-msdata";
        public const string MetadataNamespacePrefix = "msdata";

        public static async Task WriteStartSchemaElementAsync(this XmlWriter writer, string name)
        {
            await writer.WriteStartElementAsync(SchemaNamespacePrefix, name, null);
        }

        public static async Task WriteAttributeStringAsync(this XmlWriter writer, string name, string value)
        {
            await writer.WriteAttributeStringAsync(null, name, null, value);
        }

        public static async Task WriteXmlNsAsync(this XmlWriter writer, string prefix, string @namespace)
        {
            await writer.WriteAttributeStringAsync("xmlns", prefix, null, @namespace);
        }

        public static async Task WriteStartMetadataElementAsync(this XmlWriter writer, string name)
        {
            await writer.WriteStartElementAsync(MetadataNamespacePrefix, name, null);
        }

        public static async Task WriteMedataAttributeAsync(this XmlWriter writer, string name, string value)
        {
            await writer.WriteAttributeStringAsync(MetadataNamespacePrefix, name, null, value);
        }
    }
}
