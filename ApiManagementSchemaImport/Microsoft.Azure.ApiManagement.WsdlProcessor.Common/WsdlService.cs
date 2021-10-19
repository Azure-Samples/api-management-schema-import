// --------------------------------------------------------------------------
//  <copyright file="WsdlService.cs" company="Microsoft">
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

    public class WsdlService
    {
        public XName Name { get; set; }

        public WsdlDocumentation Documentation { get; set; }

        public string Description { get; set; }

        public Dictionary<XName, WsdlEndpoint> Endpoints { get; set; }

        public WsdlInterface Interface { get; set; }

        public static WsdlService Load(WsdlDocument parent, XElement serviceElement)
        {
            var service = new WsdlService
            {
                Name = parent.TargetNamespace.GetName(serviceElement.Attribute("name").Value),
                Documentation = WsdlDocumentation.Load(serviceElement.Element(parent.WsdlNamespace + "documentation"))
            };

            switch (parent.WsdlVersion)
            {
                case WsdlVersionLiteral.Wsdl11:
                    // get bindings for this service via Port
                    service.Endpoints = serviceElement.Elements(parent.WsdlNamespace + "port")
                        .Select(p => WsdlEndpoint.Load(parent, p)).ToDictionary(k => k.Name, v => v);

                    // Grab interface from any endpoint
                    WsdlEndpoint endpoint = service.Endpoints.Values.FirstOrDefault();
                    service.Interface = endpoint?.Binding?.Interface;

                    break;

                case WsdlVersionLiteral.Wsdl20:

                    service.Interface = parent.Interfaces[WsdlDocument.MakeReferenceGlobal(serviceElement, serviceElement.Attribute("interface").Value)];

                    break;

                default:
                    throw new ArgumentException(string.Format(CommonResources.UnknownWsdlVersion, parent.WsdlNamespace?.NamespaceName));
            }

            // Update Operations from some PortBinding (which one? Does it matter?) Has to be a SOAP binding
            // look for binding with <soap:binding> attribute. Soap version?
            return service;
        }

        public void Save(XmlWriter writer)
        {
            writer.WriteStartElement("service", WsdlDocument.Wsdl11Namespace);
            writer.WriteAttributeString("name", this.Name.LocalName);
            if (this.Endpoints != null)
            {
                foreach (WsdlEndpoint endpoint in this.Endpoints.Values)
                {
                    endpoint.Save(writer);
                }
            }

            writer.WriteEndElement();
        }
    }
}