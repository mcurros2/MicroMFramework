using Microsoft.VisualStudio.TestTools.UnitTesting;
using MicroM.Core;
using System.Text;

namespace LibraryTest
{
    [TestClass]
    public class CRC32Tests
    {
        [TestMethod]
        public void Test_CRC32FromByteArray()
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes("123456789");
            uint expectedCrc = 0xCBF43926; // Correct precomputed CRC32 value for "123456789"

            uint result = CRC32.CRC32FromByteArray(inputBytes);

            Assert.AreEqual(expectedCrc, result, "CRC32FromByteArray did not return the expected value.");
        }

        [TestMethod]
        public void Test_CRCFromString()
        {
            string inputString = "123456789";
            uint expectedCrc = 0xCBF43926; // Correct precomputed CRC32 value for "123456789"

            uint result = CRC32.CRCFromString(inputString);

            Assert.AreEqual(expectedCrc, result, "CRCFromString did not return the expected value.");
        }

        [TestMethod]
        public void Test_CRCFromString_EmptyString()
        {
            string inputString = "";
            uint expectedCrc = 0x00000000; // Correct CRC32 value for an empty string

            uint result = CRC32.CRCFromString(inputString);

            Assert.AreEqual(expectedCrc, result, "CRCFromString did not return the expected value for an empty string.");
        }

        [TestMethod]
        public void Test_CRCFromString_SpecialCharacters()
        {
            string inputString = "!@#$%^&*()";
            uint expectedCrc = 0xAEA29B98; // Correct precomputed CRC32 value for "!@#$%^&*()"

            uint result = CRC32.CRCFromString(inputString);

            Assert.AreEqual(expectedCrc, result, "CRCFromString did not return the expected value for special characters.");
        }
    }
}
