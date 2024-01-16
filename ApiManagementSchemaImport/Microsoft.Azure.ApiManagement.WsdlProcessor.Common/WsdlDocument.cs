// --------------------------------------------------------------------------
//  <copyright file="WsdlDocument.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
//  </copyright>
// --------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;

using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

using Microsoft.Azure.ApiManagement.Common;

namespace Microsoft.Azure.ApiManagement.WsdlProcessor.Common
{
    // WS-I Basic Profile 1.1 : use SOAP 1.1, WSDL 1.1 and UDDI 2.0 
    // WS-I Basic Profile 1.2 (9th Nov, 2010) specifies the usage of SOAP 1.1, WSDL 1.1, UDDI 2.0, WS-Addressing 1.0 and MTOM
    // WS-I Basic Profile 2.0 (9th Nov, 2010) specifies the usage of SOAP 1.2, WSDL 1.1, UDDI 2.0, WS-Addressing and MTOM.
    // http://www.w3.org/2003/06/soap11-soap12.html

    public enum WsdlVersionLiteral
    {
        Wsdl11,
        Wsdl20  // Almost never used and currently not supported
    }

    public class WsdlDocument
    {
        public const string Wsdl11Namespace = "http://schemas.xmlsoap.org/wsdl/";

        public const string Wsdl20Namespace = "http://www.w3.org/ns/wsdl";

        public const string DefaultPrefix = "MS";

        //public IList<XAttribute> RootAttributes { get; set; }

        public static XNamespace WsdlSoap12Namespace = XNamespace.Get("http://schemas.xmlsoap.org/wsdl/soap12/");

        public static XNamespace WsdlSoap11Namespace = XNamespace.Get("http://schemas.xmlsoap.org/wsdl/soap/");

        public static XNamespace WsAddressingWsdlNamespace = XNamespace.Get("http://www.w3.org/2006/05/addressing/wsdl");

        public static XNamespace WsAddressingNamespace = XNamespace.Get("http://www.w3.org/2006/05/addressing");

        public static XNamespace XsdSchemaNamespace = XNamespace.Get("http://www.w3.org/2001/XMLSchema");

        public static XNamespace XsdSchemaInstanceNamespace = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");

        public static string[] XsdTypes = new string[] { "anyURI", "base64Binary", "boolean", "date", "dateTime", "decimal", "double", "duration", "float", "hexBinary", "gDay", "gMonth", "gMonthDay", "gYear", "gYearMonth", "NOTATION", "QName", "string", "time", "integer" };

        public XNamespace TargetNamespace { get; set; }

        public WsdlVersionLiteral WsdlVersion { get; set; }

        public XNamespace WsdlNamespace
        {
            get
            {
                return this.WsdlVersion == WsdlVersionLiteral.Wsdl11 ? Wsdl11Namespace : Wsdl20Namespace;
            }
        }

        public Dictionary<XNamespace, XElement> Schemas { get; set; }

        public Dictionary<XNamespace, HashSet<string>> Imports { get; set; }

        readonly ILog logger;

        public WsdlDocument(ILog logger)
        {
            this.WsdlVersion = WsdlVersionLiteral.Wsdl11;
            this.logger = logger;
        }

        private static XNamespace GetTargetNamespace(XElement element)
        {
            XNamespace targetNamespace = XNamespace.None;
            XAttribute attr = element.Attribute("targetNamespace");

            if (attr != null)
            {
                targetNamespace = XNamespace.Get(attr.Value);
            }

            return targetNamespace;
        }

        /// <summary>
        /// Loads a WSDL Document from an <see cref="XElement"/>.
        /// </summary>
        /// <remarks>
        /// The document is not verified for support and may result in unhandled exceptions.
        /// Use of this method is preferred for trusted documents that have already been verified
        /// and do not require further verification such as documents already saved.
        /// </remarks>
        /// <param name="documentElement">The <see cref="XElement"/> containing the WSDL.</param>
        /// <param name="logger">A logger for parsing events.</param>
        /// <param name="pathSchemaLocation">A UNC Path about schemaLocations.</param>
        public static async Task LoadAsync(XElement documentElement, ILog logger, string pathSchemaLocation)
        {
            var doc = new WsdlDocument(logger)
            {
                WsdlVersion = DetermineVersion(documentElement.Name.Namespace),

                TargetNamespace = GetTargetNamespace(documentElement),

                Imports = new Dictionary<XNamespace, HashSet<string>>()
            };

            logger.Informational("WsdlIdentification", string.Format(
                CommonResources.WsdlIdentification,
                doc.WsdlVersion,
                doc.TargetNamespace.NamespaceName));

            await ProcessWsdlImports(doc, documentElement, logger);

            //doc.RootAttributes = documentElement.Attributes().Where(a => a.ToString().Contains("xmlns:")).ToList();

            XElement types = documentElement.Element(doc.WsdlNamespace + "types");

            if (types != null)
            {
                ILookup<XNamespace, XElement> targetNamespaces = types.Elements(XsdSchemaNamespace + "schema")
                            .ToLookup(
                                k =>
                                {
                                    XNamespace key = GetTargetNamespace(k);
                                    logger.Informational("LoadedSchema", string.Format(
                                        CommonResources.LoadedSchema,
                                        key.NamespaceName));
                                    return key;
                                },
                                v => v);

                // Merge duplicate schemas
                doc.Schemas = targetNamespaces.Select(s =>
                {
                    XElement schema = s.First();
                    MergeSchemas(schema, s.Skip(1).ToList());
                    return new { key = s.Key, value = schema };
                }).ToDictionary(k => k.key, v => v.value);

                await ProcessXsdImportsIncludes(doc, logger, pathSchemaLocation);

                logger.Informational("LoadedSchemas", string.Format(CommonResources.LoadedSchemas, doc.Schemas.Count));
            }
            else
            {
                doc.Schemas = new Dictionary<XNamespace, XElement>();
                logger.Warning("LoadedSchemas", CommonResources.LoadedNoSchemas);
            }

            types.Elements().Remove();

            foreach (var schema in doc.Schemas)
            {
                //if (!types.Elements(XsdSchemaNamespace + "schema").Any(i => i.ToString().Equals(schema.Value.ToString(), StringComparison.InvariantCultureIgnoreCase) || schema.Key.NamespaceName.Equals(i.Attribute("targetNamespace")?.Value)))
                //{
                types.Add(schema.Value);
                //}
            }

            //Adding imports to each schema
            foreach (var schema in types.Elements(XsdSchemaNamespace + "schema").Where(e => e.Attribute("targetNamespace") != null))
            {
                if (doc.Imports.TryGetValue(schema.Attribute("targetNamespace")?.Value, out var imports))
                {
                    foreach (var item in imports)
                    {
                        schema.AddFirst(new XElement(XsdSchemaNamespace + "import", new XAttribute("namespace", item)));
                    }
                }
            }

            //Removing third party elements from documentElement
            var listToRemove = new List<XElement>();

            var allowedPrefixes = documentElement
            .Attributes()
            .Where(a => a.ToString().Contains("xmlns:"))
            .Select(a => a.Name.LocalName)
            .ToHashSet();

            documentElement.Elements().Where(e => !e.Name.LocalName.Equals("types")).DescendantsAndSelf().
                Where(x => !x.Name.NamespaceName.Equals(Wsdl11Namespace)
                    && !x.Name.NamespaceName.Equals(XsdSchemaNamespace.NamespaceName)
                    && !allowedPrefixes.Contains(x.GetPrefixOfNamespace(x.Name.Namespace)))
                .ToList()
                .ForEach(i => listToRemove.Add(i));

            //Another approach to remove third party elements is with Schema Validation.
            listToRemove.ForEach(i => i.Remove());
        }

        /// <summary>
        /// Merges all wsdl:imports in parent WSDL.
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="documentElement"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        private static async Task ProcessWsdlImports(WsdlDocument doc, XElement documentElement, ILog logger)
        {
            var wsdlImports = documentElement.Elements(doc.WsdlNamespace + "import")
                        .Select(e => new
                        {
                            Location = e.Attribute("location").Value,
                            Namespace = e.Attribute("namespace")?.Value
                        })
                        .ToHashSet();
            documentElement.Elements(doc.WsdlNamespace + "import").Remove();
            //var rootAttributes = documentElement.Attributes().Where(a => a.ToString().Contains("xmlns:")).Select(a => a.ToString().Split('=')[0]).ToHashSet();
            var attributesToAdd = new List<XAttribute>();
            var elementsToAdd = new List<XElement>();

            while (wsdlImports.Count > 0)
            {
                var import = wsdlImports.First();
                wsdlImports.Remove(import);
                //TODO: Add log messages
                var wsdlText = await GetStringDocumentFromUri(logger, import.Location);

                var importXDocument = XDocument.Parse(wsdlText);
                var xDocument = importXDocument.Root;
                var elements = xDocument.Elements().Reverse();

                //Modify the elements before adding them to WSDL parent
                AddXmlnsAndChangePrefixReferenced(documentElement, elements, xDocument.Attributes().
                    Where(a => a.ToString().Contains("xmlns:")).ToDictionary(a => a.Value, a => a.Name.LocalName));

                elementsToAdd.AddRange(elements);
                //We need to check for new wsdl:imports
                var newImports = xDocument.Elements(doc.WsdlNamespace + "import")
                        .Select(e => new
                        {
                            Location = e.Attribute("location").Value,
                            Namespace = e.Attribute("namespace")?.Value
                        })
                        .ToHashSet();
                wsdlImports.Union(newImports);
            }

            elementsToAdd.ForEach(i => documentElement.AddFirst(i));
            ChangeElementsToParentTargetNamespace(doc, documentElement);
        }

        /// <summary>
        /// Process all XSD Imports and Includes
        /// </summary>
        /// <param name="doc">doc.Schemas where all the imports are added</param>
        /// <param name="logger"></param>
        /// <param name="pathSchemaLocation"></param>
        /// <returns></returns>
        private static async Task ProcessXsdImportsIncludes(WsdlDocument doc, ILog logger, string pathSchemaLocation)
        {
            if (doc.Schemas == null)
            {
                return;
            }
            var schemaNames = new HashSet<string>();
            // Walk the schemas looking for imports of other schemas
            var schemasToProcess = doc.Schemas
                .SelectMany(e => e.Value.Elements(XsdSchemaNamespace + "import"))
                .Where(e => e != null && e.Attribute("schemaLocation") != null)
                .Select(i => new
                {
                    TargetNamespace = i.Attribute("namespace")?.Value,
                    SchemaLocation = i.Attribute("schemaLocation")?.Value,
                    Type = "import",
                    SchemaDirectory = pathSchemaLocation ?? Directory.GetCurrentDirectory()
                })
                .ToList();

            //Adding includes in 
            schemasToProcess.AddRange(doc.Schemas
                .SelectMany(e => e.Value.Elements(XsdSchemaNamespace + "include"))
                .Where(e => e != null && e.Attribute("schemaLocation") != null)
                .Select(i => new
                {
                    TargetNamespace = i.Parent.Attribute("namespace")?.Value,
                    SchemaLocation = i.Attribute("schemaLocation")?.Value,
                    Type = "include",
                    SchemaDirectory = pathSchemaLocation ?? Directory.GetCurrentDirectory()
                })
                .ToList());
            //Includes and imports removed from schemas

            schemasToProcess.ForEach(i => schemaNames.Add(i.SchemaLocation));

            foreach (var item in doc.Schemas)
            {
                item.Value.Elements(XsdSchemaNamespace + "include").Remove();
                item.Value.Elements(XsdSchemaNamespace + "import").Remove();
            }
            // Resolve the schemas and add to existing ones
            while (schemasToProcess.Count > 0)
            {
                var import = schemasToProcess.First();

                var realSchemaDirectory = import.SchemaLocation != string.Empty
                ? import.SchemaLocation
                : Path.GetDirectoryName(Path.GetFullPath(Path.Join(import.SchemaDirectory, import.SchemaLocation)));

                schemasToProcess.Remove(import);
                XmlSchema xmlSchema;

                logger.Informational("XsdImportInclude", string.Format(
                    CommonResources.XsdImport,
                    import.SchemaLocation,
                    import.TargetNamespace));

                string schemaText = string.Empty;

                if (pathSchemaLocation != null && pathSchemaLocation != string.Empty)
                {
                    schemaText = Path.Join(pathSchemaLocation, import.SchemaLocation);
                }
                else
                {
                    schemaText = await GetStringDocumentFromUri(logger, import.SchemaLocation);
                }

                xmlSchema = GetXmlSchema(schemaText, logger);
                var includesToRemove = new List<XmlSchemaExternal>();
                var importsToAdd = new HashSet<string>();

                foreach (XmlSchemaExternal item in xmlSchema.Includes)
                {
                    if (item is XmlSchemaImport || item is XmlSchemaInclude)
                    {
                        var schemaLocation = item.SchemaLocation;

                        if (!schemaNames.Contains(item.SchemaLocation))
                        {
                            var xmlTargetNamespace = xmlSchema.TargetNamespace;
                            string itemType;

                            if (item is XmlSchemaImport importItem)
                            {
                                logger.Informational("import");
                                itemType = "import";
                                xmlTargetNamespace = importItem.Namespace;
                            }
                            else
                            {
                                logger.Informational("include");
                                itemType = "include";
                            }

                            //All new imports are added
                            importsToAdd.Add(xmlTargetNamespace);
                            schemaNames.Add(item.SchemaLocation);

                            var newSchemaToProcess = new
                            {
                                TargetNamespace = xmlTargetNamespace,
                                SchemaLocation = schemaLocation,
                                Type = itemType,
                                SchemaDirectory = realSchemaDirectory
                            };

                            schemasToProcess.Add(newSchemaToProcess);
                            logger.Informational(
                                @$"SchemaLocation: {newSchemaToProcess.SchemaLocation},
                                 SchemaDirectory: {newSchemaToProcess.SchemaDirectory}");
                        }

                        if (item is XmlSchemaImport)
                        {
                            item.SchemaLocation = null;
                        }

                        includesToRemove.Add(item);
                    }
                    else
                    {
                        throw new WsdlDocumentException(CommonResources.SchemaOnlyAllowsIncludeOrImport);
                    }
                }

                includesToRemove.ForEach(x => xmlSchema.Includes.Remove(x));

                var sw = new StringWriter();
                xmlSchema.Write(sw);
                schemaText = sw.ToString();

                var schemaElement = XElement.Parse(schemaText, LoadOptions.SetLineInfo);
                //BEGIN track schema comment
                schemaElement.AddFirst(new XComment(string.Format(CommonResources.XsdImportBegin, import.SchemaLocation)));
                schemaElement.Add(new XComment(string.Format(CommonResources.XsdImportEnd, import.SchemaLocation)));
                //END track schema comment
                XNamespace targetNamespace = import.TargetNamespace ?? GetTargetNamespace(schemaElement);

                if (doc.Schemas.ContainsKey(targetNamespace))
                {
                    XElement existingSchema = doc.Schemas[targetNamespace];
                    var existingSchemaXmlns = existingSchema.Attribute("xmlns")?.Value;
                    var schemaelementXmlns = schemaElement.Attribute("xmlns")?.Value;

                    if (existingSchemaXmlns != null && schemaelementXmlns != null && !existingSchemaXmlns.Equals(schemaelementXmlns))
                    {
                        if (existingSchema.HasElements)
                            throw new WsdlDocumentException(
                                $"The xmlns attribute is not the same between schemas with targetNamespace {targetNamespace.NamespaceName}");

                        existingSchema.Parent.Element(existingSchema.Name).Remove();
                        doc.Schemas[targetNamespace] = schemaElement;
                    }
                    else
                    {
                        MergeSchemas(existingSchema, new List<XElement>() { schemaElement });
                    }
                }
                else
                {
                    doc.Schemas.Add(targetNamespace, schemaElement);

                }
                importsToAdd.Remove(targetNamespace.NamespaceName);

                if (doc.Imports.ContainsKey(targetNamespace))
                {
                    doc.Imports[targetNamespace].UnionWith(importsToAdd);
                }
                else
                {
                    doc.Imports[targetNamespace] = importsToAdd;
                }
            }
        }

        /// <summary>
        /// Get string document from uri (URL or path)
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="location"></param>
        /// <returns>string document</returns>
        private static async Task<string> GetStringDocumentFromUri(ILog logger, string location)
        {
            string documentText;
            //We need to check if is URL or a File location
            var result = Uri.TryCreate(location, UriKind.Absolute, out var uriResult)
                    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            if (result)
            {
                var clientHandler = new HttpClientHandler();
                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                HttpResponseMessage response = null;

                using (var httpClient = new HttpClient(clientHandler))
                {
                    var uri = new Uri(location, UriKind.RelativeOrAbsolute);

                    try
                    {
                        response = await httpClient.GetAsync(uri);

                        documentText = await response.Content.ReadAsStringAsync();

                        if (response.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            // NOTE(daviburg): when the status code was failed, the return string if one is an error message rather than the schema.
                            logger.Error("FailedToImport", string.Format(
                                CommonResources.FailedToImport,
                                uri.OriginalString,
                                response.StatusCode,
                                documentText));

                            throw new Exception("Failed to import schema from external url");
                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        // NOTE(daviburg): this can happen when the content type header charset value of the response is invalid.
                        logger.Error("FailedToImport", string.Format(
                            CommonResources.FailedToParseImportedSchemaResponse,
                            uri.OriginalString,
                            response.StatusCode,
                            nameof(InvalidOperationException),
                            ex.StackTrace));
                        throw;
                    }
                    catch (HttpRequestException ex)
                    {
                        logger.Error("FailedToImport from external url", string.Format(
                            CommonResources.FailedToImport,
                            uri.OriginalString,
                            nameof(HttpRequestException),
                            ex.StackTrace));
                        throw;
                    }
                    catch (Exception ex)
                    {
                        logger.Error("FailedToImport from external url", string.Format(
                            CommonResources.FailedToImport,
                            uri.OriginalString,
                            nameof(Exception),
                            ex.StackTrace));
                        throw;
                    }
                }
            }
            else
            {
                var importLocation = Path.IsPathRooted(location) ? location : Path.Join(Directory.GetCurrentDirectory(), location);
                documentText = File.ReadAllText(importLocation);
            }

            return documentText;
        }

        /// <summary>
        /// Add namespaces to documentElement.
        /// Then, It changes newElements attributes that have prefixes.
        /// </summary>
        /// <param name="documentElement">XElement where the method will add New namespaces</param>
        /// <param name="newElements">List of XElements where attributes value are going to change if have prefixes</param>
        /// <param name="namespaces">Namespaces of the newElements</param>
        private static void AddXmlnsAndChangePrefixReferenced(
            XElement documentElement,
            IEnumerable<XElement> newElements,
            Dictionary<string, string> namespaces)
        {
            var parentNamespaces = documentElement
            .Attributes()
            .Where(a => a.ToString()
            .Contains("xmlns:"))
            .Select(a => new
            {
                Prefix = a.Name.LocalName,
                Namespace = a.Value
            }).ToDictionary(a => a.Namespace, a => a.Prefix);

            foreach (var item in namespaces)
            {
                //string prefix = string.Empty;
                parentNamespaces.TryGetValue(item.Key, out string prefix);

                if (prefix == null)
                {
                    prefix = GenerateNewNamespace(documentElement, parentNamespaces, item);
                }

                //Modify attributes from elements with the same prefix
                var prefixValue = item.Value + ":";

                foreach (var element in newElements.DescendantsAndSelf())
                {
                    //Go through all attributes and modify if they have the prefix
                    var attributes = element
                    .Attributes()
                    .Where(e => e.Value.Contains(prefixValue));

                    foreach (var attribute in attributes)
                    {
                        if (attribute.Value.Count(i => i == ':') == 1
                        && !(Uri.TryCreate(attribute.Value, UriKind.Absolute, out var uriResult)
                        && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)))
                        {
                            var splitValue = attribute.Value.Split(':');
                            attribute.Value = prefix + ":" + splitValue[1];
                        }
                    }
                }
            }
        }

        /// <summary>
        /// It generates a new xmlns:prefix and add it to documentElement
        /// </summary>
        /// <param name="documentElement">XElement to add new xmlns</param>
        /// <param name="parentNamespaces">All namespaces of XElement<Namespace, prefix></param>
        /// <param name="item">Namespace, prefix KeyValuepair</param>
        /// <returns></returns>
        private static string GenerateNewNamespace(
            XElement documentElement,
            Dictionary<string, string> parentNamespaces,
            KeyValuePair<string, string> item)
        {
            var newNamespaces = parentNamespaces.Values.Where(v => v.Contains(WsdlDocument.DefaultPrefix));
            var initialValue = 1;

            if (newNamespaces.Any())
            {
                initialValue = newNamespaces.Select(i => i.Substring(2)).Max(i => int.Parse(i)) + 1;
            }

            var newPrefix = DefaultPrefix + initialValue.ToString();
            documentElement.Add(new XAttribute(XNamespace.Xmlns + newPrefix, item.Key));
            parentNamespaces.Add(item.Key, newPrefix);

            return newPrefix;
        }

        /// <summary>
        /// Change WSDL elements to Parent targetNamespace/Prefix
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="documentElement">Wsdl merged</param>
        private static void ChangeElementsToParentTargetNamespace(WsdlDocument doc, XElement documentElement)
        {
            //Searching for wsdl:portType and getting all the inputs, outputs and faults
            var prefixParentNamespace = documentElement.GetPrefixOfNamespace(documentElement.Attribute("targetNamespace").Value);
            var operationChildren = documentElement
            .Elements(doc.WsdlNamespace + "portType")
            .Elements(doc.WsdlNamespace + "operation")
            .Elements()
            .Where(e => e.Name.LocalName.Equals("input")
                || e.Name.LocalName.Equals("output")
                || e.Name.LocalName.Equals("fault"));

            foreach (var item in operationChildren)
            {
                var attribute = item.Attribute("message");

                if (attribute != null
                && attribute.Value.Count(i => i == ':') == 1
                && !(Uri.TryCreate(attribute.Value, UriKind.Absolute, out var uriResult)
                        && (uriResult.Scheme == Uri.UriSchemeHttp
                        || uriResult.Scheme == Uri.UriSchemeHttps)))
                {
                    var splitValue = attribute.Value.Split(':');
                    attribute.Value = prefixParentNamespace + ":" + splitValue[1];
                }
            }

            //Searching for binding/type
            var bindingsType = documentElement.Elements(doc.WsdlNamespace + "binding");

            foreach (var item in bindingsType)
            {
                var attribute = item.Attribute("type");

                if (attribute != null
                && attribute.Value.Count(i => i == ':') == 1
                && !(Uri.TryCreate(attribute.Value, UriKind.Absolute, out var uriResult)
                        && (uriResult.Scheme == Uri.UriSchemeHttp
                        || uriResult.Scheme == Uri.UriSchemeHttps)))
                {
                    var splitValue = attribute.Value.Split(':');
                    attribute.Value = prefixParentNamespace + ":" + splitValue[1];
                }
            }
        }

        private static void MergeSchemas(XElement schema, IList<XElement> schemas)
        {
            foreach (XElement dupSchema in schemas)
            {
                foreach (XAttribute attribute in dupSchema.Attributes())
                {
                    var schemaAttribute = schema.Attribute(attribute.Name);

                    if (schemaAttribute == null)
                    {
                        schema.Add(new XAttribute(attribute.Name, attribute.Value));
                    }
                    else
                    {
                        if (schemaAttribute.Name.LocalName.Equals(attribute.Name.LocalName) &&
                            !schemaAttribute.Value.Equals(attribute.Value))
                        {
                            AddXmlnsAndChangePrefixReferenced(
                                schema,
                                dupSchema.Elements(),
                                new Dictionary<string, string>()
                                {
                                    { attribute.Value, attribute.Name.LocalName }
                                });
                        }
                    }
                }
                schema.Add(dupSchema.Elements());
            }
        }

        private static WsdlVersionLiteral DetermineVersion(XNamespace wsdlNS)
        {
            return wsdlNS.NamespaceName switch
            {
                Wsdl11Namespace => WsdlVersionLiteral.Wsdl11,
                Wsdl20Namespace => WsdlVersionLiteral.Wsdl20,
                _ => throw new DocumentParsingException(string.Format(CommonResources.UnknownWsdlVersion, wsdlNS.NamespaceName))
            };
        }

        private static XmlSchema GetXmlSchema(string xmlSchema, ILog logger)
        {
            try
            {
                if (File.Exists(xmlSchema))
                {
                    xmlSchema = new StreamReader(xmlSchema).ReadToEnd();
                }

                using (var doc = new StringReader(xmlSchema))
                {
                    var result = XmlSchema.Read(doc, null);
                    return result;
                }
            }
            catch (XmlException ex)
            {
                logger.Error("GetXmlSchema XmlException", ex);
                throw;
            }
            catch (XmlSchemaException ex)
            {
                logger.Error("GetXmlSchema XmlSchemaException", ex);
                throw;
            }
        }
    }
}