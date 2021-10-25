// --------------------------------------------------------------------------
//  <copyright file="WsdlInterface.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
//  </copyright>
// --------------------------------------------------------------------------

namespace Microsoft.Azure.ApiManagement.WsdlProcessor.Common
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;

    public class WsdlInterface
    {
        public XName Name { get; set; }
        public Dictionary<XName, WsdlOperation> Operations { get; set; }

        internal static WsdlInterface Load(WsdlDocument doc, XElement m)
        {
            var targetNamespace = m.Attributes("parentTargetNamespace").Count() > 0 ? m.Attribute("parentTargetNamespace").Value : doc.TargetNamespace;
            var iface = new WsdlInterface
            {
                Name = targetNamespace + m.Attribute("name").Value,

                // Get operations from portType because we don't necessarily know which Binding will be used
                Operations = m.Elements(doc.WsdlNamespace + "operation")
                                    .Select(o => WsdlOperation.Load(doc, o)).ToDictionary(k => k.Name, v => v)
            };

            return iface;
        }

        internal void Save(XmlWriter writer)
        {
            writer.WriteStartElement("portType", WsdlDocument.Wsdl11Namespace);
            writer.WriteAttributeString("name", this.Name.LocalName);
            foreach (WsdlOperation operation in this.Operations.Values)
            {
                operation.Save(writer);
            }

            writer.WriteEndElement();
        }
    }
}