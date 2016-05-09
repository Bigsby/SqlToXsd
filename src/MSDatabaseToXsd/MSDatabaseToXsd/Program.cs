using System;
using System.Linq;
using static System.Console;


namespace MSDatabaseToXsd
{
    class Program
    {
        private const int _baseParametersCount = 4;
        private const int _extraParametersCount = 1;

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

                WriteLine($"{schema.Tables.Count()} tables found.");
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

            return args[_baseParametersCount] == "f" || args[_baseParametersCount] == "foreignKeys";
        }

        private static void ShowParametersError()
        {
            Error.WriteLine("Invalid Paramters!!!");
            ShowUsage();
            Environment.Exit(1);
        }

        private static void ShowUsage()
        {
            WriteLine("»»» MS SQL Database to XSD - Schema generator «««");
            WriteLine("Usage:");
            WriteLine("\tDatabaseToXsd connectionstring schemaId dataSetName targetFile [foreignKeys]");
            WriteLine();
            WriteLine("Example:");
            WriteLine("\tDatabaseToXsd server=.\\sqexpress;database=DatabaseName;uid=sa;pwd=password OmeviewManagementSchema TheDataSet TheSchema.xsd [f]");
        }
        #endregion
    }
}
