// --------------------------------------------------------------------------
//  <copyright file="WsdlMessage.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
//  </copyright>
// --------------------------------------------------------------------------

namespace Microsoft.Azure.ApiManagement.WsdlProcessor.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;

    public class WsdlMessage
    {
        public XName Name { get; set; }

        public WsdlType Type { get; set; }

        public WsdlDocumentation Documentation { get; set; }

        public XNamespace WsdlNamespace { get; set; }

        public Uri SoapAction { get; set; }

        public Dictionary<string, WsdlType> Parts { get; set; } = new Dictionary<string, WsdlType>();

        public WsdlMessage(XNamespace wsdlNamespace)
        {
            this.WsdlNamespace = wsdlNamespace;
        }

        public static WsdlMessage Load(WsdlDocument parent, XElement messageElement)
        {
            XAttribute messageUnqualifiedName = messageElement.Attribute("name");
            if (messageUnqualifiedName == null)
            {
                //throw new DocumentParsingException(string.Format(Properties.Resources.WsdlMessageNameAttributeNotFound, messageElement.Name));
            }

            var message = new WsdlMessage(parent.WsdlNamespace)
            {
                Name = parent.TargetNamespace + messageUnqualifiedName.Value,
                Documentation = WsdlDocumentation.Load(messageElement.Element(parent.WsdlNamespace + "Documentation"))
            };

            XAttribute soapActionAttribute = messageElement.Attribute(WsdlDocument.WsAddressingWsdlNamespace + "Action");
            if (soapActionAttribute != null)
            {
                message.SoapAction = new Uri(soapActionAttribute.Value, UriKind.RelativeOrAbsolute);
            }

            IEnumerable<XElement> parts = messageElement.Elements(XNamespace.Get(WsdlDocument.Wsdl11Namespace) + "part");

            foreach (XElement part in parts)
            {
                XAttribute elementAttribute = part.Attribute("element");

                // Ignore parts that don't have elements for the moment
                if (elementAttribute != null)
                {
                    var typeName = WsdlDocument.MakeReferenceGlobal(part, elementAttribute.Value);

                    WsdlType wsdlType;
                    if (!parent.Types.TryGetValue(typeName, out wsdlType))
                    {
                        //throw new DocumentParsingException(string.Format(Properties.Resources.CannotResolveType, typeName));
                    }

                    message.Parts.Add(part.Attribute("name").Value, wsdlType);
                }
            }

            if (message.Parts.Count > 0)
            {
                message.Type = message.Parts.First().Value;  // Use first part for common case
            }

            return message;
        }

        public void Save(XmlWriter writer)
        {
            writer.WriteStartElement("message", this.WsdlNamespace.NamespaceName);
            writer.WriteAttributeString("name", this.Name.LocalName);
            if (this.Documentation != null)
            {
                this.Documentation.Save(writer);
            }

            if (this.Type != null)
            {
                writer.WriteStartElement("part", this.WsdlNamespace.NamespaceName);
                writer.WriteAttributeString("name", "parameters");
                writer.WriteAttributeString("element", "tns:" + this.Type.Name.LocalName);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }
    }
}