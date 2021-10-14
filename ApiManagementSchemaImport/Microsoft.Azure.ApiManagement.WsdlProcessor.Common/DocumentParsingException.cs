// --------------------------------------------------------------------------
//  <copyright file="DocumentParsingException.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
//  </copyright>
// --------------------------------------------------------------------------

namespace Microsoft.Azure.ApiManagement.WsdlProcessor.Common
{
    using System;

    [Serializable]
    public class DocumentParsingException : Exception
    {
        public DocumentParsingException(string message) : base(message)
        {
        }

        public DocumentParsingException(string message, string report) : base(message)
        {
        }
    }
}
