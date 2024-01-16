using Microsoft.Azure.ApiManagement.WsdlProcessor.Common;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TestConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var log = new ConsoleLog();
            var wsdlfile = "API-0427";
            var wsdlString = File.ReadAllText(wsdlfile + ".wsdl");
            var xDocument = XDocument.Parse(wsdlString);
            await WsdlDocument.LoadAsync(xDocument.Root, log, string.Empty);
            //WsdlDocument.DumpInvalidNodes(xDocument.Root);
            xDocument.Root.Save(wsdlfile + "-processed.wsdl");
            //FileStream fs = new FileStream(@"C:\Temp\" + wsdlfile + "-processed.wsdl", FileMode.Create);
            //wsdlDocument.Save(XmlWriter.Create(fs, new XmlWriterSettings() { Indent = true }));
            //fs.Close();
            Console.ReadLine();
        }
    }
}
