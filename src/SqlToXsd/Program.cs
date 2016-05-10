using System;
using System.Linq;
using static System.Console;

namespace SqlToXsd
{
    class Program
    {
        #region Parameter fields
        private const int _baseParametersCount = 4;
        private const int _extraParametersCount = 1;

        private static string[] _helpParameters = new[]
        {
            "-h",
            "help",
            "-help",
            "\\help"
        };

        private static string[] _foreignKeysParameters = new[]
        {
            "foreignkeys",
            "-foreignKeys",
            "fk",
            "-fk"
        };
        #endregion

        static void Main(string[] args)
        {
            if (args.Length == 1 && _helpParameters.Contains(args[0].ToLowerInvariant()))
            {
                ShowUsage();
                return;
            }

            if (args.Length < _baseParametersCount || args.Length > _baseParametersCount + _extraParametersCount)
                ShowParametersError();

            var connectionString = args[0];
            var schameId = args[1];
            var dataSetName = args[2];
            var targetFile = args[3];
            var addForeignKeys = ParseForeignKeysParameter(args);

            try
            {
                WriteLine("Reading schema from database...");

                var schema = SchemaReader.ReadSchema(connectionString, addForeignKeys);

                WriteLine("Found:");
                WriteLine($"{schema.Tables.Count()} Tables.");
                WriteLine($"{schema.PrimaryKeys.Count()} Primary Keys.");
                if (addForeignKeys)
                    WriteLine($"{schema.ForeignKeys.Count()} Foreign Keys.");
                WriteLine();
                WriteLine("Writing schema to file...");

                SchemaWriter.WriteSchema(schema, targetFile, schameId, dataSetName, addForeignKeys);

                WriteLine("Done!");
            }
            catch (Exception ex)
            {
                Error.WriteLine("Error!!!");
                Error.WriteLine(ex.Message);
                Error.WriteLine(ex.StackTrace);
            }
        }

        #region Usage
        private static bool ParseForeignKeysParameter(string[] args)
        {
            if (args.Length == _baseParametersCount)
                return false;

            return _foreignKeysParameters.Contains(args[_baseParametersCount].ToLowerInvariant());
        }

        private static void ShowParametersError()
        {
            Error.WriteLine("Invalid Paramters!!!");
            ShowUsage();
            Environment.Exit(1);
        }

        private static void ShowUsage()
        {
            var name = $"{typeof(Program).Namespace}.exe";

            WriteLine("»»» MS SQL Database to XSD - Schema generator «««");
            WriteLine("Usage:");
            WriteLine($"\t{name} connectionstring schemaId dataSetName targetFile [foreignKeys]");
            WriteLine();
            WriteLine("Example:");
            WriteLine($"\t{name} server=.\\sqlexpress;database=DatabaseName;uid=sa;pwd=password theSchema theDataSet theSchema.xsd [fk]");
        }
        #endregion
    }
}
