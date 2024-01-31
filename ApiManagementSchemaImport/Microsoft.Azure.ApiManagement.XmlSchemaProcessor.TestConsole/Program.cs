using Microsoft.Azure.ApiManagement.XmlSchemaProcessor.Common;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Azure.ApiManagement.XmlSchemaProcessor.TestConsole
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var log = new ConsoleLog();
            var directoryPath = @"C:\proj\GMFiles\XSD with Include\GetProvideShipments";
            await XmlSchemaDocument.LoadAsync(directoryPath, log, Path.Combine(directoryPath, "myoutput"));
        }
    }
}
