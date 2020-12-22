using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using NUnit.Framework;
using Org.BouncyCastle.Utilities.IO;
using Org.BouncyCastle.Utilities.Test;

namespace Org.BouncyCastle.Bcpg.OpenPgp.Tests
{
    [TestFixture]
    public class PgpPbeTest
        : SimpleTest
    {
        private static readonly DateTime TestDateTime = new DateTime(2003, 8, 29, 23, 35, 11, 0);

        private static readonly byte[] enc1 = Convert.FromBase64String(
            "jA0EAwMC5M5wWBP2HBZgySvUwWFAmMRLn7dWiZN6AkQMvpE3b6qwN3SSun7zInw2"
            + "hxxdgFzVGfbjuB8w");
        //        private static readonly byte[] enc1crc = Convert.FromBase64String("H66L");
        private static readonly string pass = "hello world";

        /**
		 * Message with both PBE and symmetric
		 */
        private static readonly byte[] testPBEAsym = Convert.FromBase64String(
            "hQIOA/ZlQEFWB5vuEAf/covEUaBve7NlWWdiO5NZubdtTHGElEXzG9hyBycp9At8" +
            "nZGi27xOZtEGFQo7pfz4JySRc3O0s6w7PpjJSonFJyNSxuze2LuqRwFWBYYcbS8/" +
            "7YcjB6PqutrT939OWsozfNqivI9/QyZCjBvFU89pp7dtUngiZ6MVv81ds2I+vcvk" +
            "GlIFcxcE1XoCIB3EvbqWNaoOotgEPT60unnB2BeDV1KD3lDRouMIYHfZ3SzBwOOI" +
            "6aK39sWnY5sAK7JjFvnDAMBdueOiI0Fy+gxbFD/zFDt4cWAVSAGTC4w371iqppmT" +
            "25TM7zAtCgpiq5IsELPlUZZnXKmnYQ7OCeysF0eeVwf+OFB9fyvCEv/zVQocJCg8" +
            "fWxfCBlIVFNeNQpeGygn/ZmRaILvB7IXDWP0oOw7/F2Ym66IdYYIp2HeEZv+jFwa" +
            "l41w5W4BH/gtbwGjFQ6CvF/m+lfUv6ZZdzsMIeEOwhP5g7rXBxrbcnGBaU+PXbho" +
            "gjDqaYzAWGlrmAd6aPSj51AGeYXkb2T1T/yoJ++M3GvhH4C4hvitamDkksh/qRnM" +
            "M/s8Nku6z1+RXO3M6p5QC1nlAVqieU8esT43945eSoC77K8WyujDNbysDyUCUTzt" +
            "p/aoQwe/HgkeOTJNelKR9y2W3xinZLFzep0SqpNI/e468yB/2/LGsykIyQa7JX6r" +
            "BYwuBAIDAkOKfv5rK8v0YDfnN+eFqwhTcrfBj5rDH7hER6nW3lNWcMataUiHEaMg" +
            "o6Q0OO1vptIGxW8jClTD4N1sCNwNu9vKny8dKYDDHbCjE06DNTv7XYVW3+JqTL5E" +
            "BnidvGgOmA==");

        private byte[] DecryptMessage(byte[] message)
        {
            var encryptedMessage = (PgpEncryptedMessage)PgpMessage.ReadMessage(message);
            var compressedMessage = (PgpCompressedMessage)encryptedMessage.DecryptMessage(pass);
            var literalMessage = (PgpLiteralMessage)compressedMessage.ReadMessage();
            Assert.IsTrue(literalMessage.FileName.Equals("test.txt") || literalMessage.FileName.Equals(PgpLiteralData.Console));
            Assert.AreEqual(TestDateTime, literalMessage.ModificationTime);
            byte[] bytes = Streams.ReadAll(literalMessage.GetStream());
            if (encryptedMessage.IsIntegrityProtected)
            {
                Assert.IsTrue(encryptedMessage.Verify());
            }
            return bytes;
        }

        private byte[] EncryptMessage(byte[] msg, bool withIntegrityPacket)
        {
            MemoryStream bOut = new MemoryStream();

            PgpEncryptedDataGenerator encryptedGenerator = new PgpEncryptedDataGenerator(SymmetricKeyAlgorithmTag.Cast5, withIntegrityPacket);
            PgpCompressedDataGenerator comData = new PgpCompressedDataGenerator(CompressionAlgorithmTag.Zip);
            PgpLiteralDataGenerator lData = new PgpLiteralDataGenerator();
            encryptedGenerator.AddMethod(pass, HashAlgorithmTag.Sha1);
            using (var writer = new PacketWriter(bOut))
            using (var encryptedWriter = encryptedGenerator.Open(writer))
            using (var compressedWriter = comData.Open(encryptedWriter))
            using (var ldOut = lData.Open(compressedWriter, PgpLiteralData.Binary, PgpLiteralData.Console, TestDateTime))
                ldOut.Write(msg);

            return bOut.ToArray();
        }

        public override void PerformTest()
        {
            byte[] data = DecryptMessage(enc1);
            if (data[0] != 'h' || data[1] != 'e' || data[2] != 'l')
            {
                Fail("wrong plain text in packet");
            }

            //
            // create a PBE encrypted message and read it back.
            //
            byte[] text = Encoding.ASCII.GetBytes("hello world!\n");

            byte[] encryptedData = EncryptMessage(text, withIntegrityPacket: false);
            data = DecryptMessage(encryptedData);
            if (!AreEqual(data, text))
            {
                Fail("wrong plain text in generated packet");
            }

            //
            // with integrity packet
            //
            encryptedData = EncryptMessage(text, withIntegrityPacket: true);
            data = DecryptMessage(encryptedData);
            if (!AreEqual(data, text))
            {
                Fail("wrong plain text in generated packet");
            }

            //
            // sample message
            //
            var encryptedMessage = (PgpEncryptedMessage)PgpMessage.ReadMessage(testPBEAsym);
            var literalMessage = (PgpLiteralMessage)encryptedMessage.DecryptMessage("password");
            byte[] bytes = Streams.ReadAll(literalMessage.GetStream());
            Assert.AreEqual(Encoding.ASCII.GetBytes("Sat 10.02.07\r\n"), bytes);

            //
            // with integrity packet - one byte message
            //
            byte[] msg = new byte[1];
            encryptedData = EncryptMessage(msg, true);
            data = DecryptMessage(encryptedData);
            if (!AreEqual(data, msg))
            {
                Fail("wrong plain text in generated packet");
            }
        }

        public override string Name
        {
            get { return "PgpPbeTest"; }
        }

        [Test]
        public void TestFunction()
        {
            string resultText = Perform().ToString();

            Assert.AreEqual(Name + ": Okay", resultText);
        }
    }
}
