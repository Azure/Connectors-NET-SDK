//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;

namespace Azure.Connectors.Sdk
{
    /// <summary>
    /// Marks a parameter whose values are dynamically populated by a discovery operation at design time.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class DynamicValuesAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicValuesAttribute"/> class.
        /// </summary>
        /// <param name="operationId">The operation ID of the discovery method that provides the parameter values.</param>
        public DynamicValuesAttribute(string operationId)
        {
            if (string.IsNullOrWhiteSpace(operationId))
            {
                throw new ArgumentException(message: "Parameter 'operationId' cannot be null, empty, or whitespace.", paramName: nameof(operationId));
            }

            this.OperationId = operationId;
        }

        /// <summary>
        /// Gets the operation ID of the discovery method that provides dynamic values.
        /// </summary>
        public string OperationId { get; }
    }
}
