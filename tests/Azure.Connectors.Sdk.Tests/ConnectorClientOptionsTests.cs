//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Azure.Connectors.Sdk.Tests
{
    [TestClass]
    public class ConnectorClientOptionsTests
    {
        [TestMethod]
        public void Constructor_WithDefaultVersion_UsesLatestVersion()
        {
            var options = new ConnectorClientOptions();

            Assert.AreEqual(ConnectorClientOptions.ServiceVersion.V1, options.Version);
        }

        [TestMethod]
        public void Constructor_WithExplicitVersion_SetsVersion()
        {
            var options = new ConnectorClientOptions(ConnectorClientOptions.ServiceVersion.V1);

            Assert.AreEqual(ConnectorClientOptions.ServiceVersion.V1, options.Version);
        }

        [TestMethod]
        public void Constructor_WithReservedZeroVersion_ThrowsArgumentException()
        {
            var exception = Assert.ThrowsExactly<ArgumentException>(
                () => new ConnectorClientOptions(version: default));

            Assert.AreEqual("version", exception.ParamName);
        }
    }
}
