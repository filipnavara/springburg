using System;

namespace Org.BouncyCastle.Bcpg.Sig
{
    public class EmbeddedSignature : SignatureSubpacket
    {
        public EmbeddedSignature(bool critical, bool isLongLength, byte[] data)
            : base(SignatureSubpacketTag.EmbeddedSignature, critical, isLongLength, data)
        {
        }
    }
}
