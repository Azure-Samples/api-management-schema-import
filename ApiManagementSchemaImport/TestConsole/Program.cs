using Microsoft.Azure.ApiManagement.WsdlProcessor.Common;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
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
            await WsdlDocument.LoadAsync(xDocument.Root, log);
            //WsdlDocument.DumpInvalidNodes(xDocument.Root);
            xDocument.Root.Save(wsdlfile + "-processed.wsdl");
            //FileStream fs = new FileStream(@"C:\Temp\" + wsdlfile + "-processed.wsdl", FileMode.Create);
            //wsdlDocument.Save(XmlWriter.Create(fs, new XmlWriterSettings() { Indent = true }));
            //fs.Close();
            Console.ReadLine();
        }
    }

    public class ConsoleLog : ILog
    {
        public void Critical(string eventName)
        {
            Console.WriteLine("Critical : " + eventName);
        }

        public void Critical(string eventName, string message)
        {
            Console.WriteLine("Critical : " + eventName + " : " + message);
        }

        public void Critical(string eventName, Exception ex)
        {
            Console.WriteLine("Critical : " + eventName + " : Exception : " + ex.Message);
        }

        public void Critical(string eventName, string message, Exception ex)
        {
            Console.WriteLine("Critical : " + eventName + " : " + message + " : Exception : " + ex.Message);
        }

        public void Critical(string eventName, string message, Exception ex, params object[] args)
        {
            Console.WriteLine("Critical : " + eventName + " : " + message + " : Exception : " + ex.Message);
        }

        public void Error(string eventName)
        {
            Console.WriteLine("Error : " + eventName);
        }

        public void Error(string eventName, string message)
        {
            Console.WriteLine("Error : " + eventName + " : " + message);
        }

        public void Error(string eventName, Exception ex)
        {
            Console.WriteLine("Error : " + eventName + " : Exception : " + ex.Message);
        }

        public void Error(string eventName, string message, Exception ex)
        {
            Console.WriteLine("Error : " + eventName + " : " + message + " : Exception : " + ex.Message);
        }

        public void Error(string eventName, string message, Exception ex, params object[] args)
        {
            Console.WriteLine("Error : " + eventName + " : " + message + " : Exception : " + ex.Message);
        }

        public void Informational(string eventName)
        {
            Console.WriteLine("Info : " + eventName);
        }

        public void Informational(string eventName, string message)
        {
            Console.WriteLine("Info : " + eventName + " : " + message);
        }

        public void Informational(string eventName, Exception ex)
        {
            Console.WriteLine("Info : " + eventName + " : Exception : " + ex.Message);
        }

        public void Informational(string eventName, string message, Exception ex)
        {
            Console.WriteLine("Info : " + eventName + " : " + message + " : Exception : " + ex.Message);
        }

        public void Informational(string eventName, string message, Exception ex, params object[] args)
        {
            Console.WriteLine("Info : " + eventName + " : " + message + " : Exception : " + ex.Message);
        }

        public void Verbose(string eventName)
        {
            Console.WriteLine("Verbose : " + eventName);
        }

        public void Verbose(string eventName, string message)
        {
            Console.WriteLine("Verbose : " + eventName + " : " + message);
        }

        public void Verbose(string eventName, Exception ex)
        {
            Console.WriteLine("Verbose : " + eventName + " : Exception : " + ex.Message);
        }

        public void Verbose(string eventName, string message, Exception ex)
        {
            Console.WriteLine("Verbose : " + eventName + " : " + message + " : Exception : " + ex.Message);
        }

        public void Verbose(string eventName, string message, Exception ex, params object[] args)
        {
            Console.WriteLine("Verbose : " + eventName + " : " + message + " : Exception : " + ex.Message);
        }

        public void Warning(string eventName)
        {
            Console.WriteLine("Warning : " + eventName);
        }

        public void Warning(string eventName, string message)
        {
            Console.WriteLine("Warning : " + eventName + " : " + message);
        }

        public void Warning(string eventName, Exception ex)
        {
            Console.WriteLine("Warning : " + eventName + " : Exception : " + ex.Message);
        }

        public void Warning(string eventName, string message, Exception ex)
        {
            Console.WriteLine("Warning : " + eventName + " : " + message + " : Exception : " + ex.Message);
        }

        public void Warning(string eventName, string message, Exception ex, params object[] args)
        {
            Console.WriteLine("Warning : " + eventName + " : " + message + " : Exception : " + ex.Message);
        }
    }
}
