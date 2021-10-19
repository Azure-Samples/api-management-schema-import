// --------------------------------------------------------------------------
//  <copyright file="WsdlType.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
//  </copyright>
// --------------------------------------------------------------------------

namespace Microsoft.Azure.ApiManagement.WsdlProcessor.Common
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;

    public class WsdlType
    {
        public XNamespace TargetNamespace { get; set; }

        public XName Name { get; set; }

        public XElement SchemaElement { get; set; }

        public static IEnumerable<WsdlType> GetTypes(XElement schema)
        {
            var types = new List<WsdlType>();
            XNamespace targetNamespace = null;
            XAttribute att = schema.Attribute("targetNamespace");
            if (att != null)
            {
                targetNamespace = XNamespace.Get(schema.Attribute("targetNamespace").Value);
            }
            else
            {
                targetNamespace = XNamespace.None;
            }

            types = schema.Elements(XName.Get("{http://www.w3.org/2001/XMLSchema}element")).Select(s => WsdlType.Load(targetNamespace, s)).ToList();
            return types;
        }

        public static WsdlType Load(XNamespace targetNamespace, XElement e)
        {
            var type = new WsdlType
            {
                TargetNamespace = targetNamespace,
                Name = targetNamespace + e.Attribute("name").Value,
                SchemaElement = e
            };

            return type;
        }

        public void Save(XmlWriter writer)
        {
            // We don't serialize Types directly, we
            // serialize the top level schemas under the wsdl:Types element
        }
    }
}