// --------------------------------------------------------------------------
//  <copyright file="WsdlBinding.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
//  </copyright>
// --------------------------------------------------------------------------

namespace Microsoft.Azure.ApiManagement.WsdlProcessor.Common
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using System.Xml;

    public class WsdlBinding
    {
        public XName Name { get; set; }

        public XNamespace WsdlNamespace { get; }

        public XNamespace SoapNamespace
        {
            get
            {
                return this.OperationBindings.FirstOrDefault()?.SoapOperation?.Namespace;
            }
        }

        public WsdlInterface Interface { get; set; }

        public List<WsdlBindingOperation> OperationBindings { get; set; }

        public WsdlBinding(XNamespace wsdlNamespace)
        {
            this.WsdlNamespace = wsdlNamespace;
        }

        public static WsdlBinding Load(WsdlDocument parent, XElement bindingElement)
        {
            var binding = new WsdlBinding(parent.WsdlNamespace)
            {
                Name = parent.TargetNamespace.GetName(bindingElement.Attribute("name").Value)
            };

            XName interfaceName = WsdlDocument.MakeReferenceGlobal(bindingElement, bindingElement.Attribute("type").Value);

            WsdlInterface wsdlInterface;
            if (!parent.Interfaces.TryGetValue(interfaceName, out wsdlInterface))
            {
                //throw new DocumentParsingException(string.Format(Properties.Resources.InterfaceNotFound, interfaceName));
            }

            binding.Interface = wsdlInterface;
            binding.OperationBindings = bindingElement.Elements(binding.WsdlNamespace + "operation")
                                            .Select(o => WsdlBindingOperation.Load(parent, binding, o)).ToList();

            return binding;
        }

        public void Save(XmlWriter writer)
        {
            writer.WriteStartElement("binding", WsdlDocument.Wsdl11Namespace);
            writer.WriteAttributeString("name", this.Name.LocalName);
            writer.WriteAttributeString("type", "tns:" + this.Interface.Name.LocalName);

            writer.WriteStartElement("binding", WsdlDocument.WsdlSoap11Namespace.NamespaceName);
            writer.WriteAttributeString("transport", "http://schemas.xmlsoap.org/soap/http");
            writer.WriteEndElement();

            foreach (WsdlBindingOperation operationBinding in this.OperationBindings)
            {
                operationBinding.Save(writer);
            }

            writer.WriteEndElement();
        }
    }
}