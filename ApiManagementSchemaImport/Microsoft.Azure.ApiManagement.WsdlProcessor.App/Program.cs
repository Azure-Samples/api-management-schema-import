﻿using Microsoft.Azure.ApiManagement.WsdlProcessor.Common;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Microsoft.Azure.ApiManagement.WsdlProcessor.App
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var log = new ConsoleLog();
            string wsdlFile;
            string outputFolder;
            if (args.Length == 2)
            {
                wsdlFile = args[0];
                wsdlFile = wsdlFile.Contains(".wsdl") ? wsdlFile : wsdlFile + ".wsdl";
                outputFolder = args[1];
                outputFolder = Path.IsPathRooted(outputFolder) ? outputFolder : Path.Join(Directory.GetCurrentDirectory(), outputFolder);
            }
            else
            {
                Console.WriteLine("Please enter a wsdl file to process and output folder.");
                return;
            }
            var wsdlString = File.ReadAllText(wsdlFile);
            var xDocument = XDocument.Parse(wsdlString);
            await WsdlDocument.LoadAsync(xDocument.Root, log);
            xDocument.Root.Save(Path.Join(outputFolder, Path.GetFileNameWithoutExtension(wsdlFile) + "-processed.wsdl"));
            //Directory.GetCurrentDirectory();
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
