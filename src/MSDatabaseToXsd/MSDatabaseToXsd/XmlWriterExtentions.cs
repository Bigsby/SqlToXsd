using System.Xml;

namespace MSDatabaseToXsd
{
    public static class XmlWriterExtensions
    {
        public const string SchemaNamespace = "http://www.w3.org/2001/XMLSchema";
        public const string SchemaNamespacePrefix = "xs";

        public static void WriteStartSchemaElement(this XmlWriter writer, string name)
        {
            writer.WriteStartElement(name, SchemaNamespace);
        }
    }
}
