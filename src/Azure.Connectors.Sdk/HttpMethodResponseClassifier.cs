//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using global::Azure.Core;

namespace Azure.Connectors.Sdk
{
    internal sealed class HttpMethodResponseClassifier : ResponseClassifier
    {
        private readonly bool _retryUnsafeHttpMethods;
        private readonly Func<HttpMessage, bool>? _isOperationRetryBlocked;

        public HttpMethodResponseClassifier(bool retryUnsafeHttpMethods, Func<HttpMessage, bool>? isOperationRetryBlocked = null)
        {
            this._retryUnsafeHttpMethods = retryUnsafeHttpMethods;
            this._isOperationRetryBlocked = isOperationRetryBlocked;
        }

        public override bool IsRetriableResponse(HttpMessage message)
        {
            return this.IsRetryEligible(message) && base.IsRetriableResponse(message);
        }

        public override bool IsRetriable(HttpMessage message, Exception exception)
        {
            return this.IsRetryEligible(message) && base.IsRetriable(message, exception);
        }

        private static bool IsSafeHttpMethod(RequestMethod method)
        {
            var methodName = method.ToString();
            return
                string.Equals(methodName, "GET", StringComparison.Ordinal) ||
                string.Equals(methodName, "HEAD", StringComparison.Ordinal) ||
                string.Equals(methodName, "OPTIONS", StringComparison.Ordinal) ||
                string.Equals(methodName, "TRACE", StringComparison.Ordinal);
        }

        private bool IsRetryEligible(HttpMessage message)
        {
            return
                this._isOperationRetryBlocked?.Invoke(message) != true &&
                (this._retryUnsafeHttpMethods ||
                 HttpMethodResponseClassifier.IsSafeHttpMethod(message.Request.Method));
        }
    }
}
