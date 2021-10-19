// --------------------------------------------------------------------------
//  <copyright file="WsdlOperation.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
//  </copyright>
// --------------------------------------------------------------------------

namespace Microsoft.Azure.ApiManagement.WsdlProcessor.Common
{
    using System.Xml;
    using System.Xml.Linq;

    public class WsdlOperation
    {
        public XName Name { get; set; }

        public WsdlMessage Input { get; set; }

        public WsdlMessage Output { get; set; }

        public WsdlDocumentation Documentation { get; set; }

        public static WsdlOperation Load(WsdlDocument parent, XElement operationElement)
        {
            var op = new WsdlOperation
            {
                Name = parent.TargetNamespace.GetName(operationElement.Attribute("name").Value),
                Documentation = WsdlDocumentation.Load(operationElement.Element(parent.WsdlNamespace + "documentation"))
            };

            // 1.1 Parse Message
            if (parent.WsdlVersion == WsdlVersionLiteral.Wsdl11)
            {
                XElement inputMessageElement = operationElement.Element(parent.WsdlNamespace + "input");
                if (inputMessageElement != null)
                {
                    XName messageName = WsdlDocument.MakeReferenceGlobal(operationElement, inputMessageElement.Attribute("message").Value);
                    WsdlMessage wsdlMessage;
                    if (!parent.Messages.TryGetValue(messageName, out wsdlMessage))
                    {
                        //throw new DocumentParsingException(string.Format(Properties.Resources.InputMessageNotFound, messageName));
                    }

                    op.Input = wsdlMessage;
                }

                XElement outputMessageElement = operationElement.Element(parent.WsdlNamespace + "output");
                if (outputMessageElement != null)
                {
                    XName messageName = WsdlDocument.MakeReferenceGlobal(operationElement, outputMessageElement.Attribute("message").Value);
                    if (!parent.Messages.ContainsKey(messageName))
                    {
                        //throw new DocumentParsingException(string.Format(Properties.Resources.OutputMessageNotFound, messageName));
                    }

                    op.Output = parent.Messages[messageName];
                }
            }
            else
            {
                // 2.0 Create Input/Output Messages
                XElement inputMessageElement = operationElement.Element(parent.WsdlNamespace + "input");
                op.Input = WsdlMessage.Load(parent, inputMessageElement);

                XElement outputMessageElement = operationElement.Element(parent.WsdlNamespace + "output");
                op.Output = WsdlMessage.Load(parent, outputMessageElement);
            }

            return op;
        }

        public void Save(XmlWriter writer)
        {
            writer.WriteStartElement("operation", WsdlDocument.Wsdl11Namespace);
            writer.WriteAttributeString("name", this.Name.LocalName);
            if (this.Input != null)
            {
                writer.WriteStartElement("input", WsdlDocument.Wsdl11Namespace);
                writer.WriteAttributeString("message", "tns:" + this.Input.Name.LocalName);
                writer.WriteEndElement();
            }

            if (this.Output != null)
            {
                writer.WriteStartElement("output", WsdlDocument.Wsdl11Namespace);
                writer.WriteAttributeString("message", "tns:" + this.Output.Name.LocalName);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }
    }
}