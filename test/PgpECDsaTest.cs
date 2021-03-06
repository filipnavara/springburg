﻿using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Springburg.Cryptography.OpenPgp;
using NUnit.Framework;

namespace Org.BouncyCastle.Bcpg.OpenPgp.Tests
{
    [TestFixture]
    public class PgpECDsaTest
    {
        private static readonly byte[] testPubKey =
            Convert.FromBase64String(
                "mFIEUb4HqBMIKoZIzj0DAQcCAwSQynmjwsGJHYJakAEVYxrm3tt/1h8g9Uksx32J" +
                "zG/ZH4RwaD0PbjzEe5EVBmCwSErRZxt/5AxXa0TEHWjya8FetDVFQ0RTQSAoS2V5" +
                "IGlzIDI1NiBiaXRzIGxvbmcpIDx0ZXN0LmVjZHNhQGV4YW1wbGUuY29tPoh6BBMT" +
                "CAAiBQJRvgeoAhsDBgsJCAcDAgYVCAIJCgsEFgIDAQIeAQIXgAAKCRDqO46kgPLi" +
                "vN1hAP4n0UApR36ziS5D8KUt7wEpBujQE4G3+efATJ+DMmY/SgEA+wbdDynFf/V8" +
                "pQs0+FtCYQ9schzIur+peRvol7OrNnc=");

        private static readonly byte[] testPrivKey =
            Convert.FromBase64String(
                "lKUEUb4HqBMIKoZIzj0DAQcCAwSQynmjwsGJHYJakAEVYxrm3tt/1h8g9Uksx32J" +
                "zG/ZH4RwaD0PbjzEe5EVBmCwSErRZxt/5AxXa0TEHWjya8Fe/gcDAqTWSUiFpEno" +
                "1n8izmLaWTy8GYw5/lK4R2t6D347YGgTtIiXfoNPOcosmU+3OibyTm2hc/WyG4fL" +
                "a0nxFtj02j0Bt/Fw0N4VCKJwKL/QJT+0NUVDRFNBIChLZXkgaXMgMjU2IGJpdHMg" +
                "bG9uZykgPHRlc3QuZWNkc2FAZXhhbXBsZS5jb20+iHoEExMIACIFAlG+B6gCGwMG" +
                "CwkIBwMCBhUIAgkKCwQWAgMBAh4BAheAAAoJEOo7jqSA8uK83WEA/ifRQClHfrOJ" +
                "LkPwpS3vASkG6NATgbf558BMn4MyZj9KAQD7Bt0PKcV/9XylCzT4W0JhD2xyHMi6" +
                "v6l5G+iXs6s2dw==");

        private static readonly string testPasswd = "test";

        private static readonly byte[] sExprKey =
            Convert.FromBase64String(
                "KDIxOnByb3RlY3RlZC1wcml2YXRlLWtleSgzOmVjYyg1OmN1cnZlMTU6YnJh"
                    + "aW5wb29sUDM4NHIxKSgxOnE5NzoEi29XCqkugtlRvONnpAVMQgfecL+Gk86O"
                    + "t8LnUizfHG2TqRrtqlMg1DdU8Z8dJWmhJG84IUOURCyjt8nE4BeeCfRIbTU5"
                    + "7CB13OqveBdNIRfK45UQnxHLO2MPVXf4GMdtKSg5OnByb3RlY3RlZDI1Om9w"
                    + "ZW5wZ3AtczJrMy1zaGExLWFlcy1jYmMoKDQ6c2hhMTg6itLEzGV4Cfg4OjEy"
                    + "OTA1NDcyKTE2OgxmufENKFTZUB72+X7AwkgpMTEyOvMWNLZgaGdlTN8XCxa6"
                    + "8ia0Xqqb9RvHgTh+iBf0RgY5Tx5hqO9fHOi76LTBMfxs9VC4f1rTketjEUKR"
                    + "f5amKb8lrJ67kKEsny4oRtP9ejkNzcvHFqRdxmHyL10ui8M8rJN9OU8ArqWf"
                    + "g22dTcKu02cpKDEyOnByb3RlY3RlZC1hdDE1OjIwMTQwNjA4VDE2MDg1MCkp"
                    + "KQ==");

        [Test]
        public void GenerateAndSign()
        {
            var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

            // generate a key ring
            string passPhrase = "test";
            var keyRingGen = new PgpKeyRingGenerator(ecdsa, "test@bouncycastle.org", passPhrase);

            PgpPublicKeyRing pubRing = keyRingGen.GeneratePublicKeyRing();
            PgpSecretKeyRing secRing = keyRingGen.GenerateSecretKeyRing();

            KeyTestHelper.SignAndVerifyTestMessage(secRing.GetSecretKey().ExtractPrivateKey(passPhrase), pubRing.GetPublicKey());

            PgpPublicKeyRing pubRingEnc = new PgpPublicKeyRing(pubRing.GetEncoded());
            Assert.AreEqual(pubRing.GetEncoded(), pubRingEnc.GetEncoded(), "public key ring encoding failed");

            PgpSecretKeyRing secRingEnc = new PgpSecretKeyRing(secRing.GetEncoded());
            Assert.AreEqual(secRing.GetEncoded(), secRingEnc.GetEncoded(), "secret key ring encoding failed");

            // try a signature using encoded key
            KeyTestHelper.SignAndVerifyTestMessage(secRing.GetSecretKey().ExtractPrivateKey(passPhrase), secRing.GetSecretKey());
        }

        [Test]
        public void PublicKeyDecode()
        {
            // Read the public key
            PgpPublicKeyRing pubKeyRing = new PgpPublicKeyRing(testPubKey);
            var firstUserId = pubKeyRing.GetPublicKey().GetUserIds().FirstOrDefault();
            Assert.NotNull(firstUserId);
            foreach (var certification in firstUserId.SelfCertifications)
            {
                Assert.IsTrue(certification.Verify());
            }
        }

        [Test]
        public void PrivateKeyDecode()
        {
            // Read the private key
            PgpSecretKeyRing secretKeyRing = new PgpSecretKeyRing(testPrivKey);
            PgpPrivateKey privKey = secretKeyRing.GetSecretKey().ExtractPrivateKey(testPasswd);
        }

        [Test]
        public void SxprDecode()
        {
            // sExpr
            PgpSecretKey key = PgpSecretKey.ParseSecretKeyFromSExpr(new MemoryStream(sExprKey, false), "test");
            KeyTestHelper.SignAndVerifyTestMessage(key.ExtractPrivateKey(""), key);
        }
    }
}
