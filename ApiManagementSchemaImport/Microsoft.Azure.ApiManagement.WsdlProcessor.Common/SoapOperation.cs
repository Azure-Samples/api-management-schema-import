// --------------------------------------------------------------------------
//  <copyright file="SoapOperation.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
//  </copyright>
// --------------------------------------------------------------------------

namespace Microsoft.Azure.ApiManagement.WsdlProcessor.Common
{
    using System;
    using System.Xml;
    using System.Xml.Linq;

    public class SoapOperation
    {
        public XNamespace Namespace { get; set; }

        public Uri SoapAction { get; set; }

        public string WebMethod { get; set; }

        public static SoapOperation Load(XElement element)
        {
            if (element == null)
            {
                return null;
            }

            var soapOperation = new SoapOperation
            {
                Namespace = element.Name.Namespace,
                WebMethod = "POST"
            };

            // Currently only support use="literal" not use="encoded"
            XAttribute soapActionAttribute = element.Attribute(name: "soapAction");
            if (soapActionAttribute != null && !string.IsNullOrWhiteSpace(soapActionAttribute.Value))
            {
                soapOperation.SoapAction = new Uri(uriString: soapActionAttribute.Value, uriKind: UriKind.RelativeOrAbsolute);
            }

            return soapOperation;
        }

        public void Save(XmlWriter writer)
        {
            writer.WriteStartElement(prefix: "soap", localName: "operation", ns: this.Namespace.NamespaceName);
            if (this.SoapAction != null)
            {
                writer.WriteAttributeString(localName: "soapAction", value: this.SoapAction.OriginalString);
            }

            writer.WriteEndElement();
        }
    }
}