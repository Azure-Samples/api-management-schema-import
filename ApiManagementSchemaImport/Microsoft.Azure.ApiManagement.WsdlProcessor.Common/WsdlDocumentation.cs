// --------------------------------------------------------------------------
//  <copyright file="WsdlDocumentation.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
//  </copyright>
// --------------------------------------------------------------------------

namespace Microsoft.Azure.ApiManagement.WsdlProcessor.Common
{
    using System;
    using System.Xml;
    using System.Xml.Linq;

    public class WsdlDocumentation
    {
        public string Text { get; set; }

        public static WsdlDocumentation Load(XElement docElement)
        {
            return (docElement == null || string.IsNullOrWhiteSpace(docElement.Value))
                ? null
                : new WsdlDocumentation { Text = docElement.Value };
        }

        public void Save(XmlWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}