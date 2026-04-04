//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Connectors.Sdk.Tests
{
    [TestClass]
    public class ConnectorConnectionTests
    {
        /// <summary>
        /// Clears all connection-related environment variables for a given prefix.
        /// </summary>
        private static void ClearConnectionEnvVars(string prefix)
        {
            Environment.SetEnvironmentVariable($"{prefix}__aiGatewayName", null);
            Environment.SetEnvironmentVariable($"{prefix}__connectionName", null);
            Environment.SetEnvironmentVariable($"{prefix}__connectionRuntimeUrl", null);
        }

        [TestMethod]
        public void Resolve_AiGatewayFormat_ReturnsCorrectOptions()
        {
            ClearConnectionEnvVars("TestConn");
            Environment.SetEnvironmentVariable("TestConn__aiGatewayName", "my-gateway");
            Environment.SetEnvironmentVariable("TestConn__connectionName", "my-connection");

            try
            {
                // Act
                var options = ConnectorConnectionResolver.Resolve("TestConn");

                // Assert
                Assert.IsTrue(options.IsAiGatewayConnection);
                Assert.IsFalse(options.IsDirectConnection);
                Assert.AreEqual("my-gateway", options.AiGatewayName);
                Assert.AreEqual("my-connection", options.ConnectionName);
                Assert.IsNull(options.ConnectionRuntimeUrl);
            }
            finally
            {
                ClearConnectionEnvVars("TestConn");
            }
        }

        [TestMethod]
        public void Resolve_DirectConnectionFormat_ReturnsCorrectOptions()
        {
            ClearConnectionEnvVars("DirectConn");
            Environment.SetEnvironmentVariable("DirectConn__connectionRuntimeUrl", "https://apihub.net/apim/office365/abc123");

            try
            {
                // Act
                var options = ConnectorConnectionResolver.Resolve("DirectConn");

                // Assert
                Assert.IsFalse(options.IsAiGatewayConnection);
                Assert.IsTrue(options.IsDirectConnection);
                Assert.AreEqual("https://apihub.net/apim/office365/abc123", options.ConnectionRuntimeUrl);
                Assert.IsNull(options.AiGatewayName);
                Assert.IsNull(options.ConnectionName);
            }
            finally
            {
                ClearConnectionEnvVars("DirectConn");
            }
        }

        [TestMethod]
        public void Resolve_NoSettingsFound_ThrowsArgumentException()
        {
            ClearConnectionEnvVars("Missing");

            try
            {
                // Act & Assert
                var exception = Assert.ThrowsException<ArgumentException>(
                    () => ConnectorConnectionResolver.Resolve("Missing"));

                Assert.IsTrue(exception.Message.Contains("No connection settings found", StringComparison.OrdinalIgnoreCase));
                Assert.AreEqual("connectionSettingName", exception.ParamName);
            }
            finally
            {
                ClearConnectionEnvVars("Missing");
            }
        }

        [TestMethod]
        public void Resolve_PartialAiGateway_OnlyGatewayName_ThrowsPartialMessage()
        {
            ClearConnectionEnvVars("Partial");
            Environment.SetEnvironmentVariable("Partial__aiGatewayName", "my-gateway");

            try
            {
                var exception = Assert.ThrowsException<ArgumentException>(
                    () => ConnectorConnectionResolver.Resolve("Partial"));

                Assert.IsTrue(
                    exception.Message.Contains("Partial AI Gateway", StringComparison.OrdinalIgnoreCase),
                    $"Expected partial AI Gateway message but got: {exception.Message}");
            }
            finally
            {
                ClearConnectionEnvVars("Partial");
            }
        }

        [TestMethod]
        public void Resolve_PartialAiGateway_OnlyConnectionName_ThrowsPartialMessage()
        {
            ClearConnectionEnvVars("OnlyConn");
            Environment.SetEnvironmentVariable("OnlyConn__connectionName", "my-connection");

            try
            {
                var exception = Assert.ThrowsException<ArgumentException>(
                    () => ConnectorConnectionResolver.Resolve("OnlyConn"));

                Assert.IsTrue(
                    exception.Message.Contains("Partial AI Gateway", StringComparison.OrdinalIgnoreCase),
                    $"Expected partial AI Gateway message but got: {exception.Message}");
            }
            finally
            {
                ClearConnectionEnvVars("OnlyConn");
            }
        }

        [TestMethod]
        public void Resolve_BothFormatsPresent_BothFlagsAreTrue()
        {
            ClearConnectionEnvVars("Both");
            Environment.SetEnvironmentVariable("Both__aiGatewayName", "my-gateway");
            Environment.SetEnvironmentVariable("Both__connectionName", "my-conn");
            Environment.SetEnvironmentVariable("Both__connectionRuntimeUrl", "https://apihub.net/apim/office365/abc");

            try
            {
                var options = ConnectorConnectionResolver.Resolve("Both");

                Assert.IsTrue(options.IsAiGatewayConnection);
                Assert.IsTrue(options.IsDirectConnection);
                Assert.AreEqual("my-gateway", options.AiGatewayName);
                Assert.AreEqual("my-conn", options.ConnectionName);
                Assert.AreEqual("https://apihub.net/apim/office365/abc", options.ConnectionRuntimeUrl);
            }
            finally
            {
                ClearConnectionEnvVars("Both");
            }
        }

        [TestMethod]
        public void Resolve_NullConnectionSettingName_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () => ConnectorConnectionResolver.Resolve(null!));
        }

        [TestMethod]
        public void Resolve_EmptyConnectionSettingName_ThrowsArgumentException()
        {
            Assert.ThrowsException<ArgumentException>(
                () => ConnectorConnectionResolver.Resolve(string.Empty));
        }

        [TestMethod]
        public void Resolve_WhitespaceConnectionSettingName_ThrowsArgumentException()
        {
            Assert.ThrowsException<ArgumentException>(
                () => ConnectorConnectionResolver.Resolve("   "));
        }

        [TestMethod]
        public void Options_DefaultValues_AllPropertiesNull()
        {
            var options = new ConnectorConnectionOptions();

            Assert.IsNull(options.AiGatewayName);
            Assert.IsNull(options.ConnectionName);
            Assert.IsNull(options.ConnectionRuntimeUrl);
            Assert.IsFalse(options.IsAiGatewayConnection);
            Assert.IsFalse(options.IsDirectConnection);
        }
    }
}
