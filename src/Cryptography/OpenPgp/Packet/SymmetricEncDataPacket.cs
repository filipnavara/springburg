using System;
using System.IO;

namespace InflatablePalace.Cryptography.OpenPgp.Packet
{
    public class SymmetricEncDataPacket : StreamablePacket
    {
        public override PacketTag Tag => PacketTag.SymmetricKeyEncrypted;

        public override void EncodeHeader(Stream bcpgOut)
        {
        }
    }
}