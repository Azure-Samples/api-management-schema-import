using Microsoft.Azure.ApiManagement.XmlSchemaProcessor.Common;
using System.Threading.Tasks;

namespace Microsoft.Azure.ApiManagement.XmlSchemaProcessor.App
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var log = new ConsoleLog();

            if (args.Length == 2)
            {
                var xsdDirectory = args[0];
                var outputDirectory = args[1];
                await XmlSchemaDocument.LoadAsync(xsdDirectory, log, outputDirectory);
            }
            else
            {
                log.Error("XSD Directory path and output directory should be provided as a parameter of the tool.");
            }
        }
    }
}
