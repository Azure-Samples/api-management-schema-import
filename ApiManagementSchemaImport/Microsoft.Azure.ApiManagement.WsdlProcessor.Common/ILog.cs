// --------------------------------------------------------------------------
//  <copyright file="ILog.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
//  </copyright>
// --------------------------------------------------------------------------

namespace Microsoft.Azure.ApiManagement.WsdlProcessor.Common
{
    using System;

    public interface ILog
    {
        void Verbose(string eventName);

        void Verbose(string eventName, string message);

        void Verbose(string eventName, Exception ex);

        void Verbose(string eventName, string message, Exception ex);

        void Verbose(string eventName, string message, Exception ex, params object[] args);

        void Informational(string eventName);

        void Informational(string eventName, string message);

        void Informational(string eventName, Exception ex);

        void Informational(string eventName, string message, Exception ex);

        void Informational(string eventName, string message, Exception ex, params object[] args);

        void Warning(string eventName);

        void Warning(string eventName, string message);

        void Warning(string eventName, Exception ex);

        void Warning(string eventName, string message, Exception ex);

        void Warning(string eventName, string message, Exception ex, params object[] args);

        void Error(string eventName);

        void Error(string eventName, string message);

        void Error(string eventName, Exception ex);

        void Error(string eventName, string message, Exception ex);

        void Error(string eventName, string message, Exception ex, params object[] args);

        void Critical(string eventName);

        void Critical(string eventName, string message);

        void Critical(string eventName, Exception ex);

        void Critical(string eventName, string message, Exception ex);

        void Critical(string eventName, string message, Exception ex, params object[] args);
    }
}
