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
        public static Dictionary<string, string> Schemas { get; set; }

        public static async Task LoadAsync(string directoryPath, ILog logger)
        {
            Schemas = new Dictionary<string, string>();
            var schemaPaths = Directory.GetFiles(directoryPath, "*.xsd");
            await GenerateNewNameForSchemas(schemaPaths, logger);

        }

        /// <summary>
        /// Process all XSD Imports and Includes
        /// </summary>
        /// <param name="schemaPaths">doc.Schemas where all the imports are added</param>
        /// <param name="logger"></param>
        /// <returns></returns>
        private static async Task ProcessXsdImportsIncludes(string[] schemaPaths, ILog logger)
        {
            foreach (var item in schemaPaths)
            {
                var schemaLocations = new List<string>();
                schemaLocations.Add(item);
                while (schemaLocations.Any())
                {
                    var schema = schemaLocations.First();
                    schemaLocations.Remove(schema);
                    var documentText = await GetStringDocumentFromPath(schema, logger);
                    var xmlSchema = GetXmlSchema(documentText);
                    foreach (XmlSchemaExternal import in xmlSchema.Includes)
                    {
                        if (import is XmlSchemaImport || import is XmlSchemaInclude)
                        {
                            schemaLocations.Add(import.SchemaLocation);
                            import.SchemaLocation = await NormalizeWellFormedArmSchema(import.SchemaLocation, logger);
                        }
                        else
                        {
                            //Throw exception
                        }
                    }
                }
            }
        }

        private static async Task GenerateNewNameForSchemas(string schemaPaths, ILog logger)
        {
            await GenerateNewNameForSchemas(new string[] { schemaPaths }, logger);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="schemaPaths"></param>
        /// <returns></returns>
        private static async Task GenerateNewNameForSchemas(string[] schemaPaths, ILog logger)
        {
            foreach (var item in schemaPaths)
            {
                var schemaNameWithoutExtension = Path.GetFileNameWithoutExtension(item);
                var rgx = new Regex("[^a-zA-Z0-9 -]");
                Schemas[item] = rgx.Replace(schemaNameWithoutExtension, "-");
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
            var importLocation = Path.IsPathRooted(location) ? location : Path.Join(Directory.GetCurrentDirectory(), location);
            documentText = await File.ReadAllTextAsync(importLocation);

            return documentText;
        }

        private async static Task<string> NormalizeWellFormedArmSchema(string schema, ILog logger)
        {
            Schemas.TryGetValue(schema, out var schemaNewName);
            if (string.IsNullOrEmpty(schemaNewName))
            {
                await GenerateNewNameForSchemas(schema, logger);
                schemaNewName = Schemas[schema];
            }
            return string.Join(SchemaPath, schemaNewName);
        }

        private static XmlSchema GetXmlSchema(string xmlSchema)
        {
            try
            {
                using (var doc = new StringReader(xmlSchema))
                {
                    var result = XmlSchema.Read(doc, null);
                    if (string.IsNullOrEmpty(result.TargetNamespace))
                    {
                        //Throw exception
                    }
                    return result;
                }
            }
            catch (XmlException)
            {
                throw;
            }
            catch (XmlSchemaException)
            {
                throw;
            }
        }
    }
}
