using Springburg.IO;
using System;
using System.IO;
using System.Text;

namespace Springburg.Cryptography.OpenPgp.Packet
{
    class LiteralDataPacket : StreamablePacket
    {
        private PgpDataFormat format;
        private byte[] fileName;
        private long modificationTime;

        internal LiteralDataPacket(Stream bcpgIn)
        {
            format = (PgpDataFormat)bcpgIn.ReadByte();
            int len = bcpgIn.ReadByte();

            fileName = new byte[len];
            if (len > 0 && bcpgIn.ReadFully(fileName) != len)
                throw new EndOfStreamException();

            modificationTime =
                ((uint)bcpgIn.ReadByte() << 24) |
                ((uint)bcpgIn.ReadByte() << 16) |
                ((uint)bcpgIn.ReadByte() << 8) |
                (uint)bcpgIn.ReadByte();
        }

        public LiteralDataPacket(
            PgpDataFormat format,
            string fileName,
            DateTime modificationTime)
        {
            this.format = format;
            this.fileName = Encoding.UTF8.GetBytes(fileName);
            this.modificationTime = new DateTimeOffset(modificationTime, TimeSpan.Zero).ToUnixTimeSeconds();
        }

        /// <summary>The format tag value.</summary>
        public PgpDataFormat Format => format;

        /// <summary>The modification time of the file in milli-seconds (since Jan 1, 1970 UTC)</summary>
        public DateTime ModificationTime => DateTimeOffset.FromUnixTimeSeconds(modificationTime).UtcDateTime;

        public string FileName => Encoding.UTF8.GetString(fileName);

        public ReadOnlySpan<byte> GetRawFileName() => fileName;

        public override PacketTag Tag => PacketTag.LiteralData;

        public override void EncodeHeader(Stream bcpgOut)
        {
            bcpgOut.Write(new[] {
                (byte)format,
                (byte)fileName.Length });

            bcpgOut.Write(fileName);

            bcpgOut.Write(new[] {
                (byte)(modificationTime >> 24),
                (byte)(modificationTime >> 16),
                (byte)(modificationTime >> 8),
                (byte)modificationTime });
        }
    }
}
