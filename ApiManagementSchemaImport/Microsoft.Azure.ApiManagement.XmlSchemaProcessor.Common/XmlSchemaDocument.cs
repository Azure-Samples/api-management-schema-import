using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Microsoft.Azure.ApiManagement.XmlSchemaProcessor.Common
{
    public class XmlSchemaDocument
    {
        private const string SchemaPath = "/schemas/";
        //private const string NewDirectory = "newdirectory";
        private static Dictionary<string, string> Schemas { get; set; }

        private static Dictionary<string, XmlSchema> PathXmlSchemaPair { get; set; }

        private static Dictionary<string, List<string>> PathOrderListSchemas { get; set; }

        private static string DirectoryPath { get; set; }

        public static async Task LoadAsync(string directoryPath, ILog logger, string outputDirectory)
        {
            directoryPath = Path.IsPathRooted(directoryPath) ? directoryPath : Path.Join(Directory.GetCurrentDirectory(), directoryPath);
            DirectoryPath = directoryPath;
            //outputDirectory = outputDirectory ?? NewDirectory;
            outputDirectory = Path.IsPathRooted(outputDirectory) ? outputDirectory : Path.Join(DirectoryPath, outputDirectory);
            Schemas = new Dictionary<string, string>();
            PathXmlSchemaPair = new Dictionary<string, XmlSchema>();
            PathOrderListSchemas = new Dictionary<string, List<string>>();
            logger.Informational($"Getting all xsd files from folder {directoryPath}");
            var schemaPaths = Directory.GetFiles(directoryPath, "*.xsd");
            if (!schemaPaths.Any())
            {
                logger.Error($"No xsd files in folder {directoryPath}");
                return;
            }
            if (Directory.Exists(outputDirectory) && Directory.GetFiles(outputDirectory).Length > 0)
            {
                logger.Error($"{outputDirectory} directory should not have files before executing the tool.");
                return;
            }
            else if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }
            logger.Informational($"Generating new names for schemas");
            await GenerateNewNameForSchemas(schemaPaths, logger);
            logger.Informational($"Starting process of xsd files");
            await ProcessXsdImportsIncludes(schemaPaths, logger);

            //new xsd files
            logger.Informational($"Generating new files in {outputDirectory}");
            foreach (var xmlFile in PathXmlSchemaPair.Keys)
            {
                logger.Informational($"Generating new file for {xmlFile}");
                var newSchemaFile = PathXmlSchemaPair[xmlFile];
                var file = new FileStream(Path.Join(outputDirectory, Schemas[xmlFile] + ".xsd"), FileMode.Create, FileAccess.ReadWrite);
                var xwriter = new XmlTextWriter(file, new UTF8Encoding())
                {
                    Formatting = System.Xml.Formatting.Indented
                };
                newSchemaFile.Write(xwriter);
                logger.Informational($"File generated {Path.GetFileName(file.Name)}");
            }

            var outputDictionary = new Dictionary<string, string>();
            PathOrderListSchemas.OrderByDescending(x => x.Value.Count).
                SelectMany(x => x.Value).ToList().ForEach(item => outputDictionary[Path.GetFileName(item)] = Schemas[item]);
            
            logger.Informational("Generating upload-plan.json file");
            var uploadPlan = JsonConvert.SerializeObject(outputDictionary, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(Path.Join(outputDirectory, "upload-plan.json"), uploadPlan);
        }

        /// <summary>
        /// Process all XSD Imports and Includes
        /// </summary>
        /// <param name="schemaPaths">doc.Schemas where all the imports are added</param>
        /// <param name="logger"></param>
        /// <returns></returns>
        private static async Task ProcessXsdImportsIncludes(string[] schemaPaths, ILog logger)
        {
            var visitedSchemas = new HashSet<string>();
            foreach (var item in schemaPaths)
            {
                if (!visitedSchemas.Contains(item))
                {
                    logger.Informational($"Processing parent {Path.GetFileName(item)} schema file.");
                    var orderedListOfSchemas = new List<string>();
                    if (!(await FindOrder(item, new HashSet<string>(), new HashSet<string>(), orderedListOfSchemas, logger)))
                    {
                        throw new InvalidOperationException($"There was an error trying to process {item}");
                    }
                    else
                    {
                        PathOrderListSchemas[item] = orderedListOfSchemas;
                        orderedListOfSchemas.ForEach(i => visitedSchemas.Add(i));
                    }
                }
                logger.Informational($"Schema file {Path.GetFileName(item)} is already processed.");
            }
        }

        /// <summary>
        /// Fill orderedListOfSchemas keeping the independent schemas at the beginning
        /// and the dependent schemas at the end
        /// </summary>
        /// <param name="schemaLocation"></param>
        /// <param name="importedNamespaces"></param>
        /// <param name="visitedNamespaces"></param>
        /// <param name="orderedListOfSchemas"></param>
        /// <param name="logger"></param>
        /// <returns>True if there is no problem processing. False if there is a circular dependency or a Redefine element in schema</returns>
        private async static Task<bool> FindOrder(string schemaLocation, HashSet<string> importedNamespaces, HashSet<string> visitedNamespaces, IList<string> orderedListOfSchemas, ILog logger)
        {
            logger.Informational($"Processing {Path.GetFileName(schemaLocation)} schema file.");
            if (importedNamespaces.Contains(schemaLocation))
            {
                logger.Error($"There is a circular dependency in the xml schemas. {Path.GetFileName(schemaLocation)} is in a cycle of schema dependencies");
                return false;
            }

            if (visitedNamespaces.Contains(schemaLocation))
            {
                logger.Informational($"Already visited {schemaLocation}");
                return true;
            }

            var documentText = await GetStringDocumentFromPath(schemaLocation, logger);
            var xmlSchema = GetXmlSchema(documentText, logger);
            PathXmlSchemaPair[schemaLocation] = xmlSchema;

            importedNamespaces.Add(schemaLocation);
            visitedNamespaces.Add(schemaLocation);
            logger.Informational("Processing Imports and Includes");
            foreach (XmlSchemaExternal element in xmlSchema.Includes)
            {
                string location;
                if (element is XmlSchemaImport || element is XmlSchemaInclude)
                {
                    location = Path.IsPathRooted(element.SchemaLocation) ? element.SchemaLocation : Path.Join(DirectoryPath, element.SchemaLocation);
                    element.SchemaLocation = await NormalizeWellFormedArmSchema(location, logger);
                }
                else
                {
                    logger.Error($"Redefine is not allowed in the tool.");
                    return false;
                }
                
                if (!(await FindOrder(location, importedNamespaces, visitedNamespaces, orderedListOfSchemas, logger)))
                {
                    return false;
                }
            }

            orderedListOfSchemas.Add(schemaLocation);
            importedNamespaces.Remove(schemaLocation);
            logger.Informational($"End of processing {Path.GetFileName(schemaLocation)} schema file.");
            return true;
        }

        private static async Task GenerateNewNameForSchemas(string schemaPaths, ILog logger)
        {
            await GenerateNewNameForSchemas(new string[] { schemaPaths }, logger);
        }

        /// <summary>
        /// Generates new name for schemas
        /// e.g. foo.xsd -> foo, foo_1.xsd -> foo-1, foo_1_2.xsd -> foo-1-2
        /// </summary>
        /// <param name="schemaPaths"></param>
        /// /// <param name="logger"></param>
        /// <returns></returns>
        private static async Task GenerateNewNameForSchemas(string[] schemaPaths, ILog logger)
        {
            foreach (var item in schemaPaths)
            {
                var schemaNameWithoutExtension = Path.GetFileNameWithoutExtension(item);
                logger.Informational($"Generating new name for {schemaNameWithoutExtension}.xsd");
                var rgx = new Regex("[^a-zA-Z0-9 -]");
                Schemas[item] = rgx.Replace(schemaNameWithoutExtension, "-");
                logger.Informational($"New name for {schemaNameWithoutExtension} -> {Schemas[item]}");
            }
        }

        /// <summary>
        /// Get string document from uri (URL or path)
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="location"></param>
        /// <returns>string document</returns>
        private static async Task<string> GetStringDocumentFromPath(string location, ILog logger)
        {
            string documentText;
            var importLocation = Path.IsPathRooted(location) ? location : Path.Join(DirectoryPath, location);
            documentText = await File.ReadAllTextAsync(importLocation);

            return documentText;
        }

        /// <summary>
        /// Gets the new name for schemas and generates a new location for targetnamespace in Imports and Includes
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        private async static Task<string> NormalizeWellFormedArmSchema(string schema, ILog logger)
        {
            Schemas.TryGetValue(schema, out var schemaNewName);
            if (string.IsNullOrEmpty(schemaNewName))
            {
                logger.Warning($"No new name found for {schema}");
                await GenerateNewNameForSchemas(schema, logger);
                schemaNewName = Schemas[schema];
            }
            return string.Concat(SchemaPath, schemaNewName);
        }

        private static XmlSchema GetXmlSchema(string xmlSchema, ILog logger)
        {
            try
            {
                using (var doc = new StringReader(xmlSchema))
                {
                    var result = XmlSchema.Read(doc, null);
                    if (string.IsNullOrEmpty(result.TargetNamespace))
                    {
                        var error = $"{xmlSchema} does not have targetNamespace attribute";
                        logger.Error(error);
                        throw new NullReferenceException(error);
                    }
                    return result;
                }
            }
            catch (XmlException e)
            {
                logger.Error($"{e.Message}");
                logger.Error($"Location {xmlSchema} contains invalid xml: {e.StackTrace}");
                throw;
            }
            catch (XmlSchemaException e)
            {
                logger.Error($"{e.Message}");
                logger.Error($"Location {xmlSchema} contains invalid xml: {e.StackTrace}");
                throw;
            }
        }
    }
}
