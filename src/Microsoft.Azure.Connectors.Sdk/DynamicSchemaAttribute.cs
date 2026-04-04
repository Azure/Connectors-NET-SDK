//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;

namespace Microsoft.Azure.Connectors.Sdk
{
    /// <summary>
    /// Marks a parameter or type whose schema is dynamically determined at runtime via a discovery endpoint.
    /// The discovery operation returns a JSON Schema describing the available properties based on user-selected parameters.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class DynamicSchemaAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicSchemaAttribute"/> class.
        /// </summary>
        /// <param name="operationId">The operation ID of the discovery method that provides the runtime schema.</param>
        public DynamicSchemaAttribute(string operationId)
        {
            if (string.IsNullOrWhiteSpace(operationId))
            {
                throw new ArgumentException(message: "Parameter 'operationId' cannot be null, empty, or whitespace.", paramName: nameof(operationId));
            }

            this.OperationId = operationId;
        }

        /// <summary>
        /// Gets the operation ID of the discovery method that provides the dynamic schema.
        /// </summary>
        public string OperationId { get; }
    }
}
