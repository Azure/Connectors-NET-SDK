//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

// NOTE(daviburg): These tests lock the exact name-to-wire mapping for Plumsail Timezone enum
// members affected by normalized-name collisions. The Plumsail swagger contains both Etc/GMT+N
// and Etc/GMT-N values for the same N;
// the generator now assigns semantic Plus/Minus member names so both values in each pair are
// readable and distinct (e.g., EtcGmtPlus4 for Etc/GMT+4, EtcGmtMinus4 for Etc/GMT-4).
// Sign characters are expanded to Plus/Minus only when immediately followed by a digit; ordinary
// delimiter hyphens (my-value) are unaffected. Three-way groups (+N/-N/N) resolve similarly:
// EtcGmtPlus0/EtcGmtMinus0/EtcGmt0. Singleton values without a sign counterpart keep their
// natural names (EtcGMT13, EtcGMT14).

using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlumsailTimezone = Azure.Connectors.Sdk.Plumsail.Models.Timezone;

namespace Azure.Connectors.Sdk.Tests
{
    [TestClass]
    public class GeneratedEnumMemberTests
    {
        // NOTE(daviburg): UnsafeRelaxedJsonEscaping is required: the default encoder escapes '+'
        // to '\u002B', which would cause wire-value assertions like '"Etc/GMT+4"' to fail.
        private static readonly JsonSerializerOptions RelaxedEscapeOptions = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        [TestMethod]
        public void PlumsailTimezone_EtcGmtPlus4_MapsToEtcGmtPlusFour()
        {
            // NOTE(daviburg): EtcGmtPlus4 carries the Etc/GMT+4 wire string.
            var timezone = PlumsailTimezone.EtcGmtPlus4;

            Assert.AreEqual(expected: "Etc/GMT+4", actual: (string)timezone);
            var serializedTimezone = JsonSerializer.Serialize(timezone, RelaxedEscapeOptions);
            Assert.AreEqual(expected: "\"Etc/GMT+4\"", actual: serializedTimezone);
        }

        [TestMethod]
        public void PlumsailTimezone_EtcGmtMinus4_MapsToEtcGmtMinusFour()
        {
            // NOTE(daviburg): EtcGmtMinus4 carries the Etc/GMT-4 wire string.
            var timezone = PlumsailTimezone.EtcGmtMinus4;

            Assert.AreEqual(expected: "Etc/GMT-4", actual: (string)timezone);
            var serializedTimezone = JsonSerializer.Serialize(timezone, RelaxedEscapeOptions);
            Assert.AreEqual(expected: "\"Etc/GMT-4\"", actual: serializedTimezone);
        }

        [TestMethod]
        public void PlumsailTimezone_EtcGmtMinus6_MapsToEtcGmtMinusSix()
        {
            // NOTE(daviburg): EtcGmtMinus6 carries the Etc/GMT-6 wire string.
            var timezone = PlumsailTimezone.EtcGmtMinus6;

            Assert.AreEqual(expected: "Etc/GMT-6", actual: (string)timezone);
            var serializedTimezone = JsonSerializer.Serialize(timezone, RelaxedEscapeOptions);
            Assert.AreEqual(expected: "\"Etc/GMT-6\"", actual: serializedTimezone);
        }

        [TestMethod]
        public void PlumsailTimezone_CollisionPairEtcGmt4_BothMembersDistinctWireValues()
        {
            var positiveTimezone = PlumsailTimezone.EtcGmtPlus4;
            var negativeTimezone = PlumsailTimezone.EtcGmtMinus4;

            Assert.AreNotEqual(notExpected: (string)positiveTimezone, actual: (string)negativeTimezone);
            Assert.AreEqual(expected: "Etc/GMT+4", actual: (string)positiveTimezone);
            Assert.AreEqual(expected: "Etc/GMT-4", actual: (string)negativeTimezone);
        }

        [TestMethod]
        public void PlumsailTimezone_CollisionPairEtcGmt6_BothMembersDistinctWireValues()
        {
            var positiveTimezone = PlumsailTimezone.EtcGmtPlus6;
            var negativeTimezone = PlumsailTimezone.EtcGmtMinus6;

            Assert.AreNotEqual(notExpected: (string)positiveTimezone, actual: (string)negativeTimezone);
            Assert.AreEqual(expected: "Etc/GMT+6", actual: (string)positiveTimezone);
            Assert.AreEqual(expected: "Etc/GMT-6", actual: (string)negativeTimezone);
        }

        [TestMethod]
        public void PlumsailTimezone_CollisionGroupEtcGmt0_UsesSemanticNamesForSignedValues()
        {
            Assert.AreEqual(expected: "Etc/GMT+0", actual: (string)PlumsailTimezone.EtcGmtPlus0);
            Assert.AreEqual(expected: "Etc/GMT-0", actual: (string)PlumsailTimezone.EtcGmtMinus0);
            Assert.AreEqual(expected: "Etc/GMT0", actual: (string)PlumsailTimezone.EtcGmt0);
        }

        [TestMethod]
        public void PlumsailTimezone_CollisionGroupGmt0_UsesSemanticNamesForSignedValues()
        {
            Assert.AreEqual(expected: "GMT+0", actual: (string)PlumsailTimezone.GmtPlus0);
            Assert.AreEqual(expected: "GMT-0", actual: (string)PlumsailTimezone.GmtMinus0);
            Assert.AreEqual(expected: "GMT0", actual: (string)PlumsailTimezone.Gmt0);
        }

        [TestMethod]
        public void PlumsailTimezone_SingletonEtcGmt14_KeepsNaturalMemberName()
        {
            Assert.AreEqual(expected: "Etc/GMT-14", actual: (string)PlumsailTimezone.EtcGMT14);
        }

        [TestMethod]
        public void PlumsailTimezone_WireValueRoundTrip_PreservesExactStringValue()
        {
            var wireValue = "Etc/GMT+4";

            var deserialized = JsonSerializer.Deserialize<PlumsailTimezone>($"\"{wireValue}\"");

            Assert.AreEqual(expected: wireValue, actual: (string)deserialized);
        }

        [TestMethod]
        public void PlumsailTimezone_EqualsSameWireValue_ReturnsTrue()
        {
            var namedMember = PlumsailTimezone.EtcGmtPlus4;
            var constructedMember = new PlumsailTimezone("Etc/GMT+4");

            Assert.IsTrue(namedMember == constructedMember);
            Assert.IsTrue(namedMember.Equals(constructedMember));
        }

        [TestMethod]
        public void PlumsailTimezone_EqualsDifferentWireValue_ReturnsFalse()
        {
            var plusFour = PlumsailTimezone.EtcGmtPlus4;
            var minusFour = PlumsailTimezone.EtcGmtMinus4;

            Assert.IsTrue(plusFour != minusFour);
            Assert.IsFalse(plusFour.Equals(minusFour));
        }
    }
}
