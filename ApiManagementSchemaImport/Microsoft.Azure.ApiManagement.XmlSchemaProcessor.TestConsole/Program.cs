using Microsoft.Azure.ApiManagement.XmlSchemaProcessor.Common;
using System;
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
