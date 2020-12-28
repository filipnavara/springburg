using InflatablePalace.Cryptography.OpenPgp.Packet;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace InflatablePalace.Cryptography.OpenPgp
{
    /// <summary>A PGP signature object.</summary>
    public class PgpSignature : PgpEncodable
    {
        public const int BinaryDocument = 0x00;
        public const int CanonicalTextDocument = 0x01;
        public const int StandAlone = 0x02;

        public const int DefaultCertification = 0x10;
        public const int NoCertification = 0x11;
        public const int CasualCertification = 0x12;
        public const int PositiveCertification = 0x13;

        public const int SubkeyBinding = 0x18;
        public const int PrimaryKeyBinding = 0x19;
        public const int DirectKey = 0x1f;
        public const int KeyRevocation = 0x20;
        public const int SubkeyRevocation = 0x28;
        public const int CertificationRevocation = 0x30;
        public const int Timestamp = 0x40;

        private readonly SignaturePacket sigPck;
        private readonly TrustPacket? trustPck;

        private PgpSignatureAttributes? hashedAttributes;
        private PgpSignatureAttributes? unhashedAttributes;

        internal PgpSignature(SignaturePacket sigPacket)
            : this(sigPacket, null)
        {
        }

        internal PgpSignature(SignaturePacket sigPacket, TrustPacket? trustPacket)
        {
            if (sigPacket == null)
                throw new ArgumentNullException(nameof(sigPacket));

            this.sigPck = sigPacket;
            this.trustPck = trustPacket;
        }

        public PgpSignature(byte[] detachedSignature)
            : this(new MemoryStream(detachedSignature, false))
        {
        }

        public PgpSignature(Stream detachedSignature)
        {
            var packetReader = new PacketReader(detachedSignature);
            if (packetReader.NextPacketTag() != PacketTag.Signature)
            {
                throw new PgpUnexpectedPacketException();
            }
            this.sigPck = (SignaturePacket)packetReader.ReadContainedPacket();
        }

        /// <summary>The OpenPGP version number for this signature.</summary>
        public int Version => sigPck.Version;

        /// <summary>The key algorithm associated with this signature.</summary>
        public PgpPublicKeyAlgorithm KeyAlgorithm => sigPck.KeyAlgorithm;

        /// <summary>The hash algorithm associated with this signature.</summary>
        public PgpHashAlgorithm HashAlgorithm => sigPck.HashAlgorithm;

        public bool Verify(PgpPublicKey publicKey, Stream stream, bool ignoreTrailingWhitespace = false)
        {
            var helper = new PgpSignatureTransformation(SignatureType, HashAlgorithm, ignoreTrailingWhitespace);
            new CryptoStream(stream, helper, CryptoStreamMode.Read).CopyTo(Stream.Null);
            helper.Finish(sigPck.Version, sigPck.KeyAlgorithm, sigPck.CreationTime, sigPck.GetHashedSubPackets());
            return publicKey.Verify(helper.Hash, sigPck.GetSignature(), helper.HashAlgorithm);
        }

        /// <summary>
        /// Verify the signature as certifying the passed in public key as associated
        /// with the passed in ID.
        /// </summary>
        /// <param name="id">ID the key was stored under.</param>
        /// <param name="key">The key to be verified.</param>
        /// <returns>True, if the signature matches, false otherwise.</returns>
        public bool VerifyCertification(
            PgpPublicKey masterKey,
            string id,
            PgpPublicKey pubKey)
        {
            var helper = new PgpSignatureTransformation(SignatureType, HashAlgorithm, ignoreTrailingWhitespace: false);

            Debug.Assert(masterKey.KeyId == KeyId);

            helper.UpdateWithPublicKey(pubKey);
            helper.UpdateWithIdData(0xb4, Encoding.UTF8.GetBytes(id));

            helper.Finish(sigPck);
            return masterKey.Verify(helper.Hash, sigPck.GetSignature(), helper.HashAlgorithm);
        }

        /// <summary>Verify a certification for the passed in key against the passed in master key.</summary>
        /// <param name="masterKey">The key we are verifying against.</param>
        /// <param name="pubKey">The key we are verifying.</param>
        /// <returns>True, if the certification is valid, false otherwise.</returns>
        public bool VerifyCertification(
            PgpPublicKey masterKey,
            PgpPublicKey pubKey)
        {
            var helper = new PgpSignatureTransformation(SignatureType, HashAlgorithm, ignoreTrailingWhitespace: false);

            Debug.Assert(masterKey.KeyId == KeyId);

            helper.UpdateWithPublicKey(masterKey);
            helper.UpdateWithPublicKey(pubKey);

            helper.Finish(sigPck.Version, sigPck.KeyAlgorithm, sigPck.CreationTime, sigPck.GetHashedSubPackets());
            return masterKey.Verify(helper.Hash, sigPck.GetSignature(), helper.HashAlgorithm);
        }

        /// <summary>Verify a key certification, such as revocation, for the passed in key.</summary>
        /// <param name="pubKey">The key we are checking.</param>
        /// <returns>True, if the certification is valid, false otherwise.</returns>
        public bool VerifyRevocation(PgpPublicKey pubKey)
        {
            var helper = new PgpSignatureTransformation(SignatureType, HashAlgorithm, ignoreTrailingWhitespace: false);

            Debug.Assert(pubKey.KeyId == KeyId);

            if (SignatureType != KeyRevocation && SignatureType != SubkeyRevocation)
            {
                throw new InvalidOperationException("signature is not a key signature");
            }

            helper.UpdateWithPublicKey(pubKey);

            helper.Finish(sigPck);
            return pubKey.Verify(helper.Hash, sigPck.GetSignature(), helper.HashAlgorithm);
        }

        public int SignatureType => sigPck.SignatureType;

        /// <summary>The ID of the key that created the signature.</summary>
        public long KeyId => sigPck.KeyId;

        /// <summary>The creation time of this signature.</summary>
        public DateTime CreationTime => sigPck.CreationTime;

        public PgpSignatureAttributes HashedAttributes => hashedAttributes ?? (hashedAttributes = new PgpSignatureAttributes(sigPck.GetHashedSubPackets() ?? Array.Empty<SignatureSubpacket>()));

        public PgpSignatureAttributes UnhashedAttributes => unhashedAttributes ?? (unhashedAttributes = new PgpSignatureAttributes(sigPck.GetUnhashedSubPackets() ?? Array.Empty<SignatureSubpacket>()));

        public byte[] GetSignature() => sigPck.GetSignature();

        public override void Encode(IPacketWriter outStream)
        {
            outStream.WritePacket(sigPck);

            if (trustPck != null)
            {
                outStream.WritePacket(trustPck);
            }
        }
    }
}