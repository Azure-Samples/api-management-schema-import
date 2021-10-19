// --------------------------------------------------------------------------
//  <copyright file="WsdlEndpoint.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
//  </copyright>
// --------------------------------------------------------------------------

namespace Microsoft.Azure.ApiManagement.WsdlProcessor.Common
{
    using System;
    using System.Xml;
    using System.Xml.Linq;

    public class WsdlEndpoint
    {
        public Uri Location { get; set; }

        public WsdlBinding Binding { get; set; }

        public XName Name { get; set; }

        public WsdlDocument WsdlDocument { get; internal set; }

        public XNamespace SoapNamespace { get; set; }

        public static WsdlEndpoint Load(WsdlDocument doc, XElement element)
        {
            XName bindingName = WsdlDocument.MakeReferenceGlobal(element, element.Attribute("binding").Value);

            WsdlBinding binding;
            if (!doc.Bindings.TryGetValue(bindingName, out binding))
            {
                //throw new DocumentParsingException(string.Format(Properties.Resources.BindingNotFound, bindingName.ToString()));
            }

            var endpoint = new WsdlEndpoint(doc)
            {
                Name = doc.TargetNamespace.GetName(element.Attribute("name").Value),
                Binding = binding
            };

            XElement soapAddress = element.Element(WsdlDocument.WsdlSoap11Namespace + "address")
                                ?? element.Element(WsdlDocument.WsdlSoap12Namespace + "address");
            if (soapAddress != null)
            {
                endpoint.SoapNamespace = soapAddress.Name.Namespace;
                endpoint.Location = new Uri(soapAddress.Attribute("location").Value);
            }

            return endpoint;
        }

        public WsdlEndpoint(WsdlDocument doc)
        {
            this.WsdlDocument = doc;
        }

        public void Save(XmlWriter writer)
        {
            writer.WriteStartElement("port", this.WsdlDocument.WsdlNamespace.NamespaceName);
            writer.WriteAttributeString("name", this.Name.LocalName);

            if (this.Binding != null)
            {
                writer.WriteAttributeString("binding", "tns:" + this.Binding.Name.LocalName);
            }

            if (this.Location != null)
            {
                writer.WriteStartElement("address", this.SoapNamespace.NamespaceName);
                writer.WriteAttributeString("location", this.Location.OriginalString);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }
    }
}
