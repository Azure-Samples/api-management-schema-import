// --------------------------------------------------------------------------
//  <copyright file="WsdlBindingOperation.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
//  </copyright>
// --------------------------------------------------------------------------

namespace Microsoft.Azure.ApiManagement.WsdlProcessor.Common
{
    using System;
    using System.Xml;
    using System.Xml.Linq;

    public class WsdlBindingOperation
    {
        public SoapOperation SoapOperation { get; set; }

        public WsdlOperation Operation { get; set; }

        public bool WSAddressingAction
        {
            get
            {
                return this.SoapOperation != null
                    && this.SoapOperation.SoapAction == null
                    && this.Operation?.Input?.SoapAction != null;
            }
        }

        public static WsdlBindingOperation Load(WsdlDocument parent, WsdlBinding binding, XElement bindingOperationXElement)
        {
            var bindingOperation = new WsdlBindingOperation
            {
                Operation = binding.Interface.Operations[parent.TargetNamespace + bindingOperationXElement.Attribute("name").Value],
                SoapOperation = SoapOperation.Load(bindingOperationXElement.Element(WsdlDocument.WsdlSoap11Namespace + "operation"))
            };

            if (bindingOperation.SoapOperation == null)
            {
                bindingOperation.SoapOperation = SoapOperation.Load(bindingOperationXElement.Element(WsdlDocument.WsdlSoap12Namespace + "operation"));
            }

            return bindingOperation;
        }

        public void Save(XmlWriter writer)
        {
            writer.WriteStartElement("operation", WsdlDocument.Wsdl11Namespace);
            writer.WriteAttributeString("name", this.Operation.Name.LocalName);
            // SoapOperation
            if (this.SoapOperation != null)
            {
                this.SoapOperation.Save(writer);
            }

            writer.WriteStartElement("input", WsdlDocument.Wsdl11Namespace);
            if (this.SoapOperation != null)
            {
                writer.WriteStartElement("soap", "body", this.SoapOperation.Namespace.NamespaceName);
                writer.WriteAttributeString("use", "literal");
                writer.WriteEndElement();
            }

            writer.WriteEndElement();

            writer.WriteStartElement("output", WsdlDocument.Wsdl11Namespace);
            if (this.SoapOperation != null)
            {

                writer.WriteStartElement("soap", "body", this.SoapOperation.Namespace.NamespaceName);
                writer.WriteAttributeString("use", "literal");
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndElement();
        }
    }
}