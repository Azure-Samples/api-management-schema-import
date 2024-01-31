// --------------------------------------------------------------------------
//  <copyright file="WsdlDocument.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
//  </copyright>
// --------------------------------------------------------------------------
using System;

namespace Microsoft.Azure.ApiManagement.WsdlProcessor.Common
{
    [Serializable]
    public class WsdlDocumentException : Exception
    {
        public WsdlDocumentException(string message) : base(message)
        {
        }
    }
}