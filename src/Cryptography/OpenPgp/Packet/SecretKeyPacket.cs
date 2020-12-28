using InflatablePalace.IO;
using System;
using System.IO;

namespace InflatablePalace.Cryptography.OpenPgp.Packet
{
    class SecretKeyPacket : ContainedPacket
    {
        private PublicKeyPacket pubKeyPacket;
        private readonly byte[] secKeyData;
        private S2kUsageTag s2kUsage;
        private PgpSymmetricKeyAlgorithm encAlgorithm;
        private S2k s2k;
        private byte[] iv;

        internal SecretKeyPacket(Stream bcpgIn)
        {
            if (this is SecretSubkeyPacket)
            {
                pubKeyPacket = new PublicSubkeyPacket(bcpgIn);
            }
            else
            {
                pubKeyPacket = new PublicKeyPacket(bcpgIn);
            }

            s2kUsage = (S2kUsageTag)bcpgIn.ReadByte();

            if (s2kUsage == S2kUsageTag.Checksum || s2kUsage == S2kUsageTag.Sha1)
            {
                encAlgorithm = (PgpSymmetricKeyAlgorithm)bcpgIn.ReadByte();
                s2k = new S2k(bcpgIn);
            }
            else
            {
                encAlgorithm = (PgpSymmetricKeyAlgorithm)s2kUsage;
            }

            if (!(s2k != null && s2k.Type == S2k.GnuDummyS2K && s2k.ProtectionMode == 0x01))
            {
                if (s2kUsage != 0)
                {
                    if (encAlgorithm < PgpSymmetricKeyAlgorithm.Aes128)
                    {
                        iv = new byte[8];
                    }
                    else
                    {
                        iv = new byte[16];
                    }

                    if (bcpgIn.ReadFully(iv) != iv.Length)
                        throw new EndOfStreamException();
                }
            }

            secKeyData = bcpgIn.ReadAll();
        }

        public SecretKeyPacket(
            PublicKeyPacket pubKeyPacket,
            PgpSymmetricKeyAlgorithm encAlgorithm,
            S2k s2k,
            byte[] iv,
            byte[] secKeyData)
        {
            this.pubKeyPacket = pubKeyPacket;
            this.encAlgorithm = encAlgorithm;

            if (encAlgorithm != PgpSymmetricKeyAlgorithm.Null)
            {
                this.s2kUsage = S2kUsageTag.Checksum;
            }
            else
            {
                this.s2kUsage = S2kUsageTag.None;
            }

            this.s2k = s2k;
            this.iv = (byte[])iv?.Clone();
            this.secKeyData = secKeyData;
        }

        public SecretKeyPacket(
            PublicKeyPacket pubKeyPacket,
            PgpSymmetricKeyAlgorithm encAlgorithm,
            S2kUsageTag s2kUsage,
            S2k s2k,
            byte[] iv,
            byte[] secKeyData)
        {
            this.pubKeyPacket = pubKeyPacket;
            this.encAlgorithm = encAlgorithm;
            this.s2kUsage = s2kUsage;
            this.s2k = s2k;
            this.iv = iv == null ? null : (byte[])iv.Clone();
            this.secKeyData = secKeyData;
        }

        public PgpSymmetricKeyAlgorithm EncAlgorithm => encAlgorithm;

        public S2kUsageTag S2kUsage => s2kUsage;

        public ReadOnlySpan<byte> GetIV() => iv;

        public S2k S2k => s2k;

        public PublicKeyPacket PublicKeyPacket => pubKeyPacket;

        public byte[] GetSecretKeyData() => secKeyData;

        public byte[] GetEncodedContents()
        {
            using MemoryStream bOut = new MemoryStream();
            Encode(bOut);
            return bOut.ToArray();
        }

        public override PacketTag Tag => PacketTag.SecretKey;

        public override void Encode(Stream bcpgOut)
        {
            pubKeyPacket.Encode(bcpgOut);
            bcpgOut.WriteByte((byte)s2kUsage);

            if (s2kUsage == S2kUsageTag.Checksum || s2kUsage == S2kUsageTag.Sha1)
            {
                bcpgOut.WriteByte((byte)encAlgorithm);
                s2k.Encode(bcpgOut);
            }

            if (iv != null)
            {
                bcpgOut.Write(iv);
            }

            if (secKeyData != null && secKeyData.Length > 0)
            {
                bcpgOut.Write(secKeyData);
            }
        }
    }
}