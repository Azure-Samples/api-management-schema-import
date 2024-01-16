using Microsoft.Azure.ApiManagement.WsdlProcessor.Common;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.Azure.ApiManagement.WsdlProcessor.App
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // string fileName = "tiss_ssa_30500";
            // args = new string[2];
            // args[0] = @$"C:\Proj\Avanade\UHG\MergeWSDL\{fileName}.wsdl";
            // args[1] = @$"C:\Proj\Avanade\UHG\MergeWSDL\{fileName}-final.wsdl";
            //args[2] = @"C:\Proj\Avanade\UHG\arquivos_schemas_ans_tiss\";

            string wsdlFile, outputFile, pathSchemaReference = string.Empty;

            var log = new ConsoleLog();
            int exitCode = 0;

            if (args.Length == 1 || args.Length == 2)
            {
                wsdlFile = args[0];
                if (!File.Exists(wsdlFile))
                {
                    var msg = $"The input file {wsdlFile} does not exists or we cannot access.";
                    log.Error(msg);
                    throw new Exception(msg);
                }

                wsdlFile = wsdlFile.Contains(".wsdl") ? wsdlFile : wsdlFile + ".wsdl";
                outputFile = wsdlFile.Replace(".wsdl", "-WSDLProcessed.wsdl");
                pathSchemaReference = args.Length == 2 ? args[1] : string.Empty;
            }
            else
            {
                Console.WriteLine("Please enter a wsdl file to process and output file.");
                return;
            }

            try
            {
                var wsdlString = File.ReadAllText(wsdlFile);
                var xDocument = XDocument.Parse(wsdlString);
                await WsdlDocument.LoadAsync(xDocument.Root, log, pathSchemaReference);

                xDocument.Root.Save(outputFile);

                Console.WriteLine();
                Console.WriteLine();

                log.Success($"*** WSDL file {wsdlFile} processed successfully and generate file {outputFile}.");
            }
            catch (XmlException e)
            {
                exitCode = 1;
                log.Error($"{e.Message}");
                log.Error($"Location {wsdlFile} contains invalid xml: {e.StackTrace}");
            }
            catch (Exception e)
            {
                exitCode = 1;
                log.Error($"{e.Message}");
                log.Error($"Stacktrace: {e.StackTrace}");
            }

            Environment.Exit(exitCode);
        }
    }
}