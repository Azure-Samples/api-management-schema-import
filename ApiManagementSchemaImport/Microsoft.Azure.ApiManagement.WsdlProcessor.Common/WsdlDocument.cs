// --------------------------------------------------------------------------
//  <copyright file="WsdlDocument.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
//  </copyright>
// --------------------------------------------------------------------------

namespace Microsoft.Azure.ApiManagement.WsdlProcessor.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.Schema;

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

        public static XNamespace WsdlSoap12Namespace = XNamespace.Get("http://schemas.xmlsoap.org/wsdl/soap12/");

        public static XNamespace WsdlSoap11Namespace = XNamespace.Get("http://schemas.xmlsoap.org/wsdl/soap/");

        public static XNamespace WsAddressingWsdlNamespace = XNamespace.Get("http://www.w3.org/2006/05/addressing/wsdl");

        public static XNamespace WsAddressingNamespace = XNamespace.Get("http://www.w3.org/2006/05/addressing");

        public static XNamespace XsdSchemaNamespace = XNamespace.Get("http://www.w3.org/2001/XMLSchema");

        public static XNamespace XsdSchemaInstanceNamespace = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");

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

        ILog logger;

        public WsdlDocument(ILog logger)
        {
            this.WsdlVersion = WsdlVersionLiteral.Wsdl11;
            this.logger = logger;
        }

        public static WsdlDocument Parse(string document, ILog log = null)
        {
            return LoadAsync(XElement.Parse(document, LoadOptions.SetLineInfo), log).Result;
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
        /// Loads a WSDL Document from an <see cref="XDocument"/> after verifying it is proper and supported.
        /// </summary>
        /// <remarks>Use of this method is preferred for documents that have not been verified yet.</remarks>
        /// <param name="xDocument">The <see cref="XDocument"/> containing the WSDL.</param>
        /// <param name="logger">A logger for verification and parsing events.</param>
        public static Task<WsdlDocument> LoadAsync(XDocument xDocument, ILog logger = null)
        {

            return WsdlDocument.LoadAsync(xDocument.Root, logger);
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
        public static async Task<WsdlDocument> LoadAsync(XElement documentElement, ILog logger)
        {
            var doc = new WsdlDocument(logger)
            {
                WsdlVersion = DetermineVersion(documentElement.Name.Namespace),

                TargetNamespace = GetTargetNamespace(documentElement)
            };

            logger.Informational("WsdlIdentification", string.Format(CommonResources.WsdlIdentification, doc.WsdlVersion, doc.TargetNamespace.NamespaceName));

            XElement types = documentElement.Element(doc.WsdlNamespace + "types");
            if (types != null)
            {
                ILookup<XNamespace, XElement> targetNamespaces = types.Elements(XsdSchemaNamespace + "schema")
                            .ToLookup(
                                k =>
                                {
                                    XNamespace key = GetTargetNamespace(k);
                                    logger.Informational("LoadedSchema", string.Format(CommonResources.LoadedSchema, key.NamespaceName));
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

                await ProcessXsdImportsIncludes(doc, logger);

                logger.Informational("LoadedSchemas", string.Format(CommonResources.LoadedSchemas, doc.Schemas.Count));
            }
            else
            {
                doc.Schemas = new Dictionary<XNamespace, XElement>();
                logger.Warning("LoadedSchemas", CommonResources.LoadedNoSchemas);
            }

            return doc;
        }

        private static void MergeSchemas(XElement schema, IList<XElement> schemas)
        {
            foreach (XElement dupSchema in schemas)
            {
                foreach (XAttribute attribute in dupSchema.Attributes())
                {
                    if (schema.Attribute(attribute.Name) == null)
                    {
                        schema.Add(new XAttribute(attribute.Name, attribute.Value));
                    }
                }

                schema.Add(dupSchema.Elements());
            }
        }

        private static async Task ProcessXsdImportsIncludes(WsdlDocument doc, ILog logger)
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
                    SchemaLocation = i.Attribute("schemaLocation")?.Value
                })
                .ToList();
            //Adding includes in 
            schemasToProcess.AddRange(doc.Schemas
                .SelectMany(e => e.Value.Elements(XsdSchemaNamespace + "include"))
                .Where(e => e != null && e.Attribute("schemaLocation") != null)
                .Select(i => new
                {
                    TargetNamespace = doc.TargetNamespace.NamespaceName,
                    SchemaLocation = i.Attribute("schemaLocation")?.Value
                })
                .ToList());
            schemasToProcess.ForEach(i => schemaNames.Add(i.SchemaLocation));
            // Resolve the schemas and add to existing ones
            while (schemasToProcess.Count > 0)
            {
                var import = schemasToProcess[0];
                schemasToProcess.Remove(import);
                logger.Informational("XsdImportInclude", string.Format(CommonResources.XsdImport, import.SchemaLocation, import.TargetNamespace));
                string schemaText = File.ReadAllText("API-0767 WSDL\\" + import.SchemaLocation);
                var xmlSchema = GetXmlSchema(schemaText);
                var schemaElement = XElement.Parse(schemaText, LoadOptions.SetLineInfo);
                XNamespace targetNamespace = import.TargetNamespace ?? GetTargetNamespace(schemaElement);
                if (doc.Schemas.ContainsKey(targetNamespace))
                {
                    XElement existingSchema = doc.Schemas[targetNamespace];
                    MergeSchemas(existingSchema, new List<XElement>() { schemaElement });
                }
                else
                {
                    doc.Schemas.Add(targetNamespace, schemaElement);
                }
                
                foreach (XmlSchemaExternal item in xmlSchema.Includes)
                {
                    if (item is XmlSchemaImport || item is XmlSchemaInclude)
                    {
                        var schemaLocation = item.SchemaLocation;
                        if (!schemaNames.Contains(schemaLocation))
                        {
                            var xmlTargetNamespace = xmlSchema.TargetNamespace;
                            if (item is XmlSchemaImport importItem)
                            {
                                xmlTargetNamespace = importItem.Namespace;
                            }
                            //All new imports are added
                            schemaNames.Add(schemaLocation);
                            schemasToProcess.Add(new
                            {
                                TargetNamespace = xmlTargetNamespace,
                                SchemaLocation = schemaLocation
                            });
                        }
                    }
                    else
                    {
                        //throw new CoreValidationException(CommonResources.ApiManagementSchemaOnlyAllowsIncludeOrImport);
                    }
                }
            }
        }

        public void Save(XmlWriter writer)
        {
            writer.WriteStartDocument();
            if (this.WsdlVersion == WsdlVersionLiteral.Wsdl11)
            {
                this.WriteWsdl11Document(writer);
            }
            else
            {
                WsdlDocument.WriteWsdl20Document(writer);
            }

            writer.WriteEndDocument();
            writer.Flush();
        }

        private static void WriteWsdl20Document(XmlWriter writer)
        {
            writer.WriteStartElement("description", WsdlDocument.Wsdl20Namespace);
            // Write Types
            // Write Interfaces
            // Write Bindings
            // Write Services
            writer.WriteEndElement();
        }

        private void WriteWsdl11Document(XmlWriter writer)
        {
            writer.WriteStartElement("wsdl", "definitions", WsdlDocument.Wsdl11Namespace);
            writer.WriteAttributeString("xmlns", "wsdl", null, WsdlDocument.Wsdl11Namespace);

            if (this.TargetNamespace != XNamespace.None)
            {
                writer.WriteAttributeString("xmlns", "tns", null, this.TargetNamespace.NamespaceName);
                writer.WriteAttributeString("targetNamespace", this.TargetNamespace.NamespaceName);
            }

            writer.WriteEndElement();
        }

        private static WsdlVersionLiteral DetermineVersion(XNamespace wsdlNS)
        {
            switch (wsdlNS.NamespaceName)
            {
                case Wsdl11Namespace:
                    return WsdlVersionLiteral.Wsdl11;

                case Wsdl20Namespace:
                    return WsdlVersionLiteral.Wsdl20;

                default:
                    throw new DocumentParsingException(string.Format(CommonResources.UnknownWsdlVersion, wsdlNS.NamespaceName));
            }
        }

        internal static XName MakeReferenceGlobal(XElement context, string reference)
        {
            string[] pieces = reference.Split(':');
            if (pieces.Length > 1)
            {
                return context.GetNamespaceOfPrefix(pieces[0]).GetName(pieces[1]);
            }
            else
            {
                // If no prefix then assume the default empty namespace
                return XNamespace.None + pieces[0];
            }
        }

        private static XmlSchema GetXmlSchema(string xmlSchema)
        {
            try
            {
                using (var doc = new StringReader(xmlSchema))
                {
                    var result = XmlSchema.Read(doc, null);
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

    [Serializable]
    public class WsdlDocumentException : Exception
    {
        public WsdlDocumentException(string message) : base(message)
        {
        }
    }
}